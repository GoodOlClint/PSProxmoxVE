using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Starts an LXC container on a Proxmox VE node.</para>
    /// <para type="description">
    /// Sends a start command to the specified LXC container via the Proxmox VE API.
    /// Use -Wait to block until the start task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "PveContainer", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class StartPveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the container resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to start. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the start task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Start-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Starting container {VmId} on node '{Node}'...");
            var task = containerService.StartContainer(session, Node, VmId);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, task.Node ?? Node, task.Upid!, null, null, null);
            }

            WriteObject(task);
        }
    }
}
