using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Suspends an LXC container on a Proxmox VE node.</para>
    /// <para type="description">
    /// Sends a suspend command to the specified LXC container via the Proxmox VE API.
    /// Use -Wait to block until the suspend task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Suspend, "PveContainer", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class SuspendPveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the container resides.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to suspend.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the suspend task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        /// <summary>Maximum seconds to wait for the status transition when -Wait is specified. Default 60.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Timeout in seconds for -Wait (default 60).")]
        [ValidateRange(1, 3600)]
        public int Timeout { get; set; } = 60;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Suspend-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Suspending container {VmId} on node '{Node}'...");
            var task = containerService.SuspendContainer(session, Node, VmId);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
