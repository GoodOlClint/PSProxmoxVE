using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Removes an LXC container from a Proxmox VE node.</para>
    /// <para type="description">
    /// Deletes an LXC container and, optionally, all associated storage.
    /// This operation is destructive and requires confirmation.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveContainer",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class RemovePveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the container resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to remove. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">
        /// When specified, also removes the container from HA resource configuration and replication jobs.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Remove all associated resources.")]
        public SwitchParameter Purge { get; set; }

        /// <summary>
        /// <para type="description">
        /// When specified, sends force=1 to PVE, allowing removal of running containers.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Force destroy, even if running.")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the removal task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessPveRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Remove-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Removing container {VmId} from node '{Node}'...");
            var task = containerService.RemoveContainer(session, Node, VmId, Purge.IsPresent, Force.IsPresent);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, task.Node ?? Node, task.Upid!);
            }

            WriteObject(task);
        }
    }
}
