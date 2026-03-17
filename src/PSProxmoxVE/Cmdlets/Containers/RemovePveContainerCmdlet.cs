using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Removes an LXC container from a Proxmox VE node.</para>
    /// <para type="description">
    /// Deletes an LXC container and, optionally, all associated storage.
    /// This operation is destructive and requires confirmation unless -Force is specified.
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
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to remove. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">
        /// When specified, also removes the container from HA resource configuration and replication jobs.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Purge { get; set; }

        /// <summary>
        /// <para type="description">
        /// When specified, bypasses locks and forces removal even if a lock is set on the container.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the removal task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Remove-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            var task = containerService.RemoveContainer(session, Node, VmId, Purge.IsPresent);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, task.Node ?? Node, task.Upid!, null, null, null);
            }

            WriteObject(task);
        }
    }
}
