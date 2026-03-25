using System;
using System.Management.Automation;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Joins this node to an existing cluster.</para>
    /// <para type="description">
    /// Joins the current node to an existing Proxmox VE cluster. Requires the
    /// cluster's certificate fingerprint, the hostname of an existing member,
    /// and the root password of that member. Run this on the node that should join.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "PveClusterMember", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class AddPveClusterMemberCmdlet : PveCmdletBase
    {
        /// <summary>Hostname or IP of an existing cluster member.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "Hostname or IP of an existing cluster member.")]
        public string Hostname { get; set; } = string.Empty;

        /// <summary>Certificate SHA-256 fingerprint of the cluster.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "Certificate SHA-256 fingerprint of the cluster.")]
        public string Fingerprint { get; set; } = string.Empty;

        /// <summary>Root password of the existing cluster member.</summary>
        [Parameter(Mandatory = true, HelpMessage = "Root password of the cluster member.")]
        public SecureString Password { get; set; } = new SecureString();

        /// <summary>Node ID for this node.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Corosync node ID for this node.")]
        [ValidateRange(1, int.MaxValue)]
        public int? NodeId { get; set; }

        /// <summary>Number of quorum votes for this node.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Number of quorum votes.")]
        [ValidateRange(0, int.MaxValue)]
        public int? Votes { get; set; }

        /// <summary>Force join even if already part of a cluster.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Force join even if already part of a cluster.")]
        public SwitchParameter Force { get; set; }

        /// <summary>Corosync link addresses (link0..link7).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Corosync link addresses as key=value strings (e.g. 'link0=10.0.0.1').")]
        public string[]? Links { get; set; }

        /// <summary>Wait for the join task to complete.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the join task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        private const int ReauthMaxAttempts = 10;
        private const int ReauthDelayMs = 3000;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"this node to cluster via '{Hostname}'", "Join cluster"))
                return;

            var session = GetSession();
            var service = new ClusterConfigService();

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(Password);
                var plainPassword = Marshal.PtrToStringUni(ptr) ?? string.Empty;

                var linkDict = ParseLinks(Links);

                WriteVerbose($"Joining cluster via '{Hostname}'...");
                var upid = service.JoinCluster(session, Hostname, Fingerprint, plainPassword,
                    linkDict, NodeId, Votes, Force.IsPresent ? true : (bool?)null);

                var task = new PveTask { Upid = upid, Status = "running" };

                if (Wait.IsPresent && !string.IsNullOrEmpty(upid))
                {
                    task = WaitForJoinTask(session, upid, plainPassword);
                }

                WriteObject(task);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }

        /// <summary>
        /// Waits for the join task to complete. If the ticket is invalidated mid-poll
        /// (cluster join restarts auth services), re-authenticates with retry and
        /// continues waiting.
        /// </summary>
        private PveTask WaitForJoinTask(PveSession session, string upid, string plainPassword)
        {
            var nodeName = upid.Split(':').Length > 1 ? upid.Split(':')[1] : session.Hostname;
            var taskService = new TaskService();

            try
            {
                return taskService.WaitForTask(session, nodeName, upid);
            }
            catch (PveApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                WriteVerbose("Session expired during join — waiting for auth services to restart...");
                var newSession = ReauthenticateWithRetry(session, plainPassword);
                return taskService.WaitForTask(newSession, nodeName, upid);
            }
        }

        /// <summary>
        /// Retries authentication against this node until auth services come back online
        /// after the cluster join restarts them.
        /// </summary>
        private PveSession ReauthenticateWithRetry(PveSession session, string plainPassword)
        {
            for (int attempt = 0; attempt < ReauthMaxAttempts; attempt++)
            {
                Thread.Sleep(ReauthDelayMs);
                try
                {
                    return PveAuthenticator.AuthenticateWithCredentials(
                        session.Hostname, session.Port, session.SkipCertificateCheck,
                        "root@pam", plainPassword);
                }
                catch (PveApiException retryEx) when (
                    retryEx.StatusCode == HttpStatusCode.Unauthorized ||
                    retryEx.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    WriteVerbose($"Re-auth attempt {attempt + 1}/{ReauthMaxAttempts} failed, retrying...");
                }
            }

            throw new InvalidOperationException(
                "Unable to re-authenticate after cluster join. " +
                "Auth services on this node may still be restarting.");
        }
    }
}
