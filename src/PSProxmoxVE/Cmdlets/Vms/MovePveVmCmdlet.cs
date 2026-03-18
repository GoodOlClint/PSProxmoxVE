using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Migrates a QEMU/KVM virtual machine to a different Proxmox VE node.</para>
    /// <para type="description">
    /// Performs a live or offline migration of the specified virtual machine to the target node.
    /// Use -Online for live migration (VM remains running). Use -Wait to block until the
    /// migration task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "PveVm", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class MovePveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM currently resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM to migrate. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The destination node to migrate the VM to.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string TargetNode { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">
        /// When specified, performs a live migration so the VM remains running during migration.
        /// Requires shared storage between the source and target nodes.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Online { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the migration task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} from node '{Node}' to node '{TargetNode}'", "Move-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            var task = vmService.MigrateVm(session, Node, VmId, TargetNode, Online.IsPresent);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
