using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Removes (unlinks) disks from a QEMU/KVM virtual machine.</para>
    /// <para type="description">
    /// Detaches and optionally destroys one or more disks from a virtual machine
    /// via the Proxmox VE API. This operation is destructive when -Force is used.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveVmDisk",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveVmDiskCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the VM resides.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM whose disks should be removed.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">Comma-separated list of disk IDs to unlink (e.g., "scsi0,scsi1").</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Comma-separated list of disk IDs to unlink (e.g. scsi0,scsi1).")]
        public string IdList { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">When specified, forces removal of the disk images from storage.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Force removal of disk images from storage.")]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Disks '{IdList}' on VM {VmId} (node '{Node}')", "Remove-PveVmDisk"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            WriteVerbose($"Unlinking disks '{IdList}' from VM {VmId}...");
            vmService.UnlinkDisk(session, Node, VmId, IdList, Force.IsPresent);
        }
    }
}
