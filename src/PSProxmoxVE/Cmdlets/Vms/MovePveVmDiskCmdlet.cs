using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Moves a VM disk to a different storage.</para>
    /// <para type="description">
    /// Moves the specified disk on a QEMU/KVM virtual machine to a different storage backend
    /// via the Proxmox VE API. Optionally deletes the original disk after a successful move.
    /// Use -Wait to block until the move task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "PveVmDisk", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class MovePveVmDiskCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the VM resides.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM whose disk should be moved.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The disk slot to move (e.g., "scsi0", "virtio0").</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The disk slot to move (e.g. scsi0, virtio0).")]
        public string Disk { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The target storage to move the disk to.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The target storage identifier.")]
        public string Storage { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The target disk format.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Target disk format (raw, qcow2, vmdk).")]
        [ValidateSet("raw", "qcow2", "vmdk")]
        public string? Format { get; set; }

        /// <summary>
        /// <para type="description">When specified, deletes the original disk after a successful move. Default is true.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Delete the original disk after move (default true).")]
        public SwitchParameter Delete { get; set; } = true;

        /// <summary>
        /// <para type="description">When specified, waits for the move task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Disk '{Disk}' on VM {VmId} (node '{Node}') to storage '{Storage}'", "Move-PveVmDisk"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            WriteVerbose($"Moving disk '{Disk}' on VM {VmId} to storage '{Storage}'...");
            var task = vmService.MoveDisk(session, Node, VmId, Disk, Storage, Format, Delete.IsPresent);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
