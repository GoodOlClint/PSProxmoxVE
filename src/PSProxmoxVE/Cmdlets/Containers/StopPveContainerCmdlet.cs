using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Stops (powers off) an LXC container on a Proxmox VE node.</para>
    /// <para type="description">
    /// Sends a stop command to the specified LXC container via the Proxmox VE API.
    /// This immediately terminates the container without a graceful shutdown.
    /// Use Restart-PveContainer or a guest-initiated shutdown for graceful stops.
    /// Use -Wait to block until the stop task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "PveContainer", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class StopPveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the container resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to stop. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the stop task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Stop-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            var task = containerService.StopContainer(session, Node, VmId);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, task.Node ?? Node, task.Upid!, null, null, null);
            }

            WriteObject(task);
        }
    }
}
