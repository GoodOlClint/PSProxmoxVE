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
    [Cmdlet(VerbsLifecycle.Restart, "PveContainer", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class RestartPveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the container resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to restart. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">
        /// Timeout in seconds for the graceful shutdown phase and -Wait status polling. Defaults to 60 seconds.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Timeout in seconds for -Wait (default 60).")]
        [ValidateRange(1, 3600)]
        public int Timeout { get; set; } = 60;

        /// <summary>
        /// <para type="description">When specified, waits for both shutdown and start tasks to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Restart-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Restarting container {VmId} on node '{Node}'...");

            // Graceful shutdown
            var shutdownTask = containerService.ShutdownContainer(session, Node, VmId, Timeout);

            if (Wait.IsPresent)
                WaitForStatusTransition(session, Node, shutdownTask, VmId, "stopped", Timeout, isContainer: true);

            // Start
            var startTask = containerService.StartContainer(session, Node, VmId);

            if (Wait.IsPresent)
                startTask = WaitForStatusTransition(session, Node, startTask, VmId, "running", Timeout, isContainer: true);

            WriteObject(startTask);
        }
    }
}
