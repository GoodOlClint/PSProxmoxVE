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
    [Cmdlet(VerbsLifecycle.Stop, "PveContainer", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class StopPveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the container resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to stop. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the stop task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        /// <summary>Maximum seconds to wait for the status transition when -Wait is specified. Default 60.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Timeout in seconds for -Wait (default 60).")]
        [ValidateRange(1, 3600)]
        public int Timeout { get; set; } = 60;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Stop-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Stopping container {VmId} on node '{Node}'...");
            var task = containerService.StopContainer(session, Node, VmId);

            if (Wait.IsPresent)
            {
                task = WaitForStatusTransition(session, Node, task, VmId, "stopped", Timeout, isContainer: true);
            }

            WriteObject(task);
        }
    }
}
