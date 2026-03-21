using System;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets
{
    /// <summary>
    /// Base class for all PSProxmoxVE cmdlets. Provides optional -Session parameter
    /// and a helper method to resolve and validate the active session.
    /// </summary>
    public abstract class PveCmdletBase : PSCmdlet
    {
        /// <summary>
        /// An explicit PveSession to use for this cmdlet invocation.
        /// When omitted, the module-level session stored by Connect-PveServer is used.
        /// </summary>
        [Parameter(Mandatory = false)]
        public PveSession? Session { get; set; }

        /// <summary>
        /// Returns the session to use for this cmdlet.
        /// Resolution order: -Session parameter → ModuleState.ActiveSession.
        /// Throws <see cref="PveNotConnectedException"/> if no session is available,
        /// or <see cref="PveSessionExpiredException"/> if the session ticket has expired.
        /// </summary>
        protected PveSession GetSession()
        {
            var session = Session ?? ModuleState.ActiveSession;

            if (session is null)
                throw new PveNotConnectedException();

            if (session.IsExpired)
                throw new PveSessionExpiredException();

            return session;
        }

        /// <summary>
        /// Checks the connected PVE server version against a two-tier requirement:
        /// <list type="bullet">
        /// <item><b>Introduced</b> — the API endpoint was added in this version.
        ///   If the server is older, the cmdlet emits a terminating error because
        ///   the endpoint does not exist at all.</item>
        /// <item><b>Default</b> (optional) — the feature is installed/enabled by
        ///   default since this version. If the server is between <paramref name="introducedMajor"/>
        ///   and <paramref name="defaultMajor"/>, a warning is emitted but the call
        ///   proceeds, allowing users who manually enabled the feature to succeed.</item>
        /// </list>
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="featureName">Human-readable name shown in messages (e.g. "SDN IPAM").</param>
        /// <param name="introducedMajor">Major version that introduced the API endpoint.</param>
        /// <param name="introducedMinor">Minor version that introduced the API endpoint.</param>
        /// <param name="defaultMajor">Major version where the feature is enabled by default (null to skip warning tier).</param>
        /// <param name="defaultMinor">Minor version where the feature is enabled by default.</param>
        protected void RequireVersion(
            PveSession session,
            string featureName,
            int introducedMajor,
            int introducedMinor,
            int? defaultMajor = null,
            int? defaultMinor = null)
        {
            var version = session.ServerVersion;
            if (version == null) return; // version unknown — optimistic, let the call proceed

            // Hard fail: endpoint does not exist
            if (!version.IsAtLeast(introducedMajor, introducedMinor))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new PveVersionException(introducedMajor, introducedMinor, version),
                    "PveVersionTooOld",
                    ErrorCategory.InvalidOperation,
                    null));
                return;
            }

            // Soft warning: feature exists but may not be enabled by default
            if (defaultMajor.HasValue && defaultMinor.HasValue
                && !version.IsAtLeast(defaultMajor.Value, defaultMinor.Value))
            {
                WriteWarning(
                    $"{featureName} is available since PVE {introducedMajor}.{introducedMinor} but is not enabled by default until PVE {defaultMajor}.{defaultMinor}. " +
                    $"Connected server is PVE {version}. The command will proceed, but may fail if the feature is not manually enabled.");
            }
        }

        /// <summary>
        /// Waits for a PVE task to complete, then optionally polls VM status until
        /// it matches <paramref name="expectedStatus"/>. Used by lifecycle cmdlets
        /// (Start, Stop, Suspend, Resume, etc.) when -Wait is specified.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="task">The task returned by the lifecycle API call.</param>
        /// <param name="vmid">The VM or container ID to poll.</param>
        /// <param name="expectedStatus">The expected status string (e.g. "running", "stopped", "paused").</param>
        /// <param name="timeoutSeconds">Maximum seconds to wait for the status transition. Default 60.</param>
        /// <param name="isContainer">True to poll container status instead of VM status.</param>
        /// <returns>The completed task.</returns>
        protected PveTask WaitForStatusTransition(
            PveSession session,
            string node,
            PveTask task,
            int vmid,
            string expectedStatus,
            int timeoutSeconds = 60,
            bool isContainer = false)
        {
            var taskService = new TaskService();

            // First wait for the PVE task to complete
            if (!string.IsNullOrEmpty(task.Upid))
            {
                task = taskService.WaitForTask(session, node, task.Upid, null, null, null);
            }

            // Then poll status/current until VM/container reaches the expected status.
            // We query the status/current endpoint directly instead of the list endpoint
            // because it returns qmpstatus (needed for paused state detection — PVE reports
            // status=running but qmpstatus=paused for suspended VMs).
            var statusResource = isContainer
                ? $"nodes/{node}/lxc/{vmid}/status/current"
                : $"nodes/{node}/qemu/{vmid}/status/current";

            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            using var pollClient = new PveHttpClient(session);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var json = pollClient.GetAsync(statusResource).GetAwaiter().GetResult();
                    var data = JObject.Parse(json)["data"];
                    var status = data?["status"]?.ToString();
                    var qmpStatus = data?["qmpstatus"]?.ToString();

                    // Use qmpstatus when available (more accurate for VM pause state)
                    var effectiveStatus = qmpStatus ?? status;

                    if (string.Equals(effectiveStatus, expectedStatus, StringComparison.OrdinalIgnoreCase))
                        return task;
                }
                catch
                {
                    // Ignore transient errors during polling
                }

                System.Threading.Thread.Sleep(2000);
            }

            throw new PveTaskTimeoutException(
                task.Upid ?? "unknown",
                TimeSpan.FromSeconds(timeoutSeconds));
        }
    }
}
