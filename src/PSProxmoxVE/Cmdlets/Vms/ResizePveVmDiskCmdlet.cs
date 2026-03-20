using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Resizes a disk attached to a QEMU/KVM virtual machine.</para>
    /// <para type="description">
    /// Resizes the specified disk on a virtual machine via the Proxmox VE API.
    /// Use an absolute size (e.g., "50G") to set a fixed size, or a relative size
    /// prefixed with "+" (e.g., "+10G") to grow the disk by that amount.
    /// Disk shrinking is not supported by Proxmox VE.
    /// Use -Wait to block until the resize task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Resize, "PveVmDisk", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class ResizePveVmDiskCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM whose disk should be resized. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">
        /// The disk slot to resize (e.g., "virtio0", "scsi0", "ide0", "sata0").
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The disk slot to resize (e.g. virtio0, scsi0).")]
        public string Disk { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">
        /// The new size for the disk. Use an absolute value (e.g., "50G") to set a specific size,
        /// or a "+" prefix (e.g., "+10G") to grow the disk by the specified amount.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "New disk size (e.g. 50G or +10G to grow).")]
        public string Size { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">When specified, waits for the resize task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Disk '{Disk}' on VM {VmId} (node '{Node}') to size '{Size}'", "Resize-PveVmDisk"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            WriteVerbose($"Resizing disk '{Disk}' on VM {VmId}...");
            var task = vmService.ResizeDisk(session, Node, VmId, Disk, Size);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
