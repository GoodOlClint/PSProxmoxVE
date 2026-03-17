using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Clones (copies) a QEMU/KVM virtual machine on a Proxmox VE node.</para>
    /// <para type="description">
    /// Creates a clone of an existing VM. By default, a linked clone is created when the source
    /// is a template. Use -Full to create a full independent copy. Optionally specify a target
    /// node to create the clone on a different cluster node.
    /// Use -Wait to block until the clone task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Copy, "PveVm", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class CopyPveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the source VM resides.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string SourceNode { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the source VM to clone. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The VM ID to assign to the new clone. When omitted, the next available ID is used.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? NewVmId { get; set; }

        /// <summary>
        /// <para type="description">The display name for the new clone.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? NewName { get; set; }

        /// <summary>
        /// <para type="description">The target node for the clone. Defaults to the source node.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? TargetNode { get; set; }

        /// <summary>
        /// <para type="description">
        /// When specified, creates a full independent clone instead of a linked clone.
        /// A full clone is required when the source VM is not a template.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Full { get; set; }

        /// <summary>
        /// <para type="description">Target storage pool for the full clone disks.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Storage { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the clone task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            var target = TargetNode ?? SourceNode;
            if (!ShouldProcess($"VM {VmId} on node '{SourceNode}' to new VM on node '{target}'", "Copy-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            var newid = NewVmId ?? 0;
            var task = vmService.CloneVm(session, SourceNode, VmId, newid, NewName, TargetNode, Full.IsPresent);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, task.Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
