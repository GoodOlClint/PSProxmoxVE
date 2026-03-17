using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Gracefully restarts an LXC container on a Proxmox VE node.</para>
    /// <para type="description">
    /// Performs a graceful shutdown of the container followed by a start via the Proxmox VE API.
    /// A configurable timeout controls how long to wait for the container to shut down cleanly.
    /// Use -Wait to block until both tasks complete.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Restart, "PveContainer", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class RestartPveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the container resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to restart. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">
        /// Timeout in seconds for the graceful shutdown phase. Defaults to 60 seconds.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int Timeout { get; set; } = 60;

        /// <summary>
        /// <para type="description">When specified, waits for both shutdown and start tasks to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Restart-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();
            var taskService = new TaskService();

            // Graceful shutdown
            var shutdownTask = containerService.ShutdownContainer(session, Node, VmId, Timeout);

            if (Wait.IsPresent)
                taskService.WaitForTask(session, shutdownTask.Node ?? Node, shutdownTask.Upid!, null, null, null);

            // Start
            var startTask = containerService.StartContainer(session, Node, VmId);

            if (Wait.IsPresent)
                startTask = taskService.WaitForTask(session, startTask.Node ?? Node, startTask.Upid!, null, null, null);

            WriteObject(startTask);
        }
    }
}
