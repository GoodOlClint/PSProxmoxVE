using System;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Imports a disk image into a Proxmox VE virtual machine.</para>
    /// <para type="description">
    /// Imports a disk image file into the specified VM by setting a disk configuration key
    /// with the PVE import-from syntax. The image can be:
    ///
    /// - A file already on PVE storage (e.g. a cloud image downloaded via Invoke-PveStorageDownload)
    /// - A VMDK inside an uploaded OVA archive
    /// - An absolute path to an image file on the PVE node
    ///
    /// The import runs as a background task on the PVE node. Use -Wait to block until complete.
    ///
    /// Examples:
    ///   # Import a cloud image from local ISO storage
    ///   Import-PveVmDisk -Node pve1 -VmId 100 -Disk scsi0 -TargetStorage local-lvm `
    ///       -Source 'local:iso/noble-server-cloudimg-amd64.img' -Wait
    ///
    ///   # Import a VMDK from an OVA (uploaded with content=import)
    ///   Import-PveVmDisk -Node pve1 -VmId 100 -Disk sata0 -TargetStorage local-lvm `
    ///       -Source 'local:import/appliance.ova/appliance-disk1.vmdk' -Wait
    /// </para>
    /// </summary>
    [Cmdlet(VerbsData.Import, "PveVmDisk", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class ImportPveVmDiskCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier. Accepts pipeline input from Get-PveVm.</summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// The disk bus and index to import to (e.g. "scsi0", "sata0", "virtio0", "ide0").
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "Target disk slot (e.g. scsi0, sata0, virtio0).")]
        [ValidatePattern(@"^(scsi|sata|virtio|ide)\d+$")]
        public string Disk { get; set; } = string.Empty;

        /// <summary>The target storage for the imported disk (e.g. "local-lvm", "ceph-pool").</summary>
        [Parameter(Mandatory = true, HelpMessage = "Target storage for the imported disk.")]
        public string TargetStorage { get; set; } = string.Empty;

        /// <summary>
        /// The import source. Accepted formats:
        /// - Storage reference: "local:iso/image.img", "local:import/vm.ova/disk.vmdk"
        /// - Absolute path on the node: "/var/lib/vz/images/disk.qcow2"
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Import source (e.g. local:iso/image.img or local:import/vm.ova/disk.vmdk).")]
        public string Source { get; set; } = string.Empty;

        /// <summary>Target disk format (e.g. "qcow2", "raw"). If omitted, uses the storage default.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Target disk format (qcow2, raw, vmdk).")]
        [ValidateSet("qcow2", "raw", "vmdk")]
        public string? Format { get; set; }

        /// <summary>When specified, waits for the import task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the import task to complete.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} disk {Disk} on node '{Node}'", $"Import disk from '{Source}' to '{TargetStorage}'"))
                return;

            var session = GetSession();
            RequireVersion(session, "VM disk import", 8, 1);
            var vmService = new VmService();

            WriteVerbose($"Importing disk to VM {VmId} slot {Disk}: {Source} -> {TargetStorage}...");
            var task = vmService.ImportDisk(session, Node, VmId, Disk, TargetStorage, Source, Format);

            if (Wait.IsPresent && !string.IsNullOrEmpty(task.Upid))
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
