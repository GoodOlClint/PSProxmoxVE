using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
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
    [OutputType(typeof(string))]
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
                var plainPassword = Marshal.PtrToStringUni(ptr)!;

                Dictionary<string, string>? linkDict = null;
                if (Links != null)
                {
                    linkDict = new Dictionary<string, string>();
                    foreach (var link in Links)
                    {
                        var parts = link.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                            linkDict[parts[0]] = parts[1];
                    }
                }

                WriteVerbose($"Joining cluster via '{Hostname}'...");
                var upid = service.JoinCluster(session, Hostname, Fingerprint, plainPassword,
                    linkDict, NodeId, Votes, Force.IsPresent ? true : (bool?)null);
                WriteObject(upid);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}
