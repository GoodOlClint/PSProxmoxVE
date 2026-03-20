using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Authentication;
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

            // Then poll until VM/container reaches the expected status
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    string? currentStatus;
                    if (isContainer)
                    {
                        var ct = new ContainerService().GetContainer(session, node, vmid);
                        currentStatus = ct.Status;
                    }
                    else
                    {
                        var vm = new VmService().GetVm(session, node, vmid);
                        currentStatus = vm.Status;
                    }

                    if (string.Equals(currentStatus, expectedStatus, StringComparison.OrdinalIgnoreCase))
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
