using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Allocates a new disk image on a Proxmox VE storage.</para>
    /// <para type="description">
    /// Creates a new disk image with the specified filename, size and optional format
    /// on the given storage and node. Returns the volume ID of the newly created disk.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveStorageDisk", SupportsShouldProcess = true)]
    [OutputType(typeof(string))]
    public sealed class NewPveStorageDiskCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        /// <summary>The storage identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The storage pool name.")]
        [ValidateNotNullOrEmpty]
        public string Storage { get; set; } = string.Empty;

        /// <summary>The filename for the new disk (e.g. "vm-100-disk-1").</summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "The filename for the new disk (e.g. vm-100-disk-1).")]
        [ValidateNotNullOrEmpty]
        public string Filename { get; set; } = string.Empty;

        /// <summary>The size of the disk (e.g. "32G", "100M").</summary>
        [Parameter(Mandatory = true, Position = 3, HelpMessage = "The size of the disk (e.g. 32G, 100M).")]
        [ValidateNotNullOrEmpty]
        public string Size { get; set; } = string.Empty;

        /// <summary>The disk image format.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The disk image format (raw, qcow2, vmdk).")]
        [ValidateSet("raw", "qcow2", "vmdk", IgnoreCase = true)]
        public string? Format { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"{Filename} ({Size})", "New PVE Storage Disk"))
                return;

            var config = new Dictionary<string, string>
            {
                ["filename"] = Filename,
                ["size"] = Size
            };
            if (!string.IsNullOrEmpty(Format))
                config["format"] = Format!;

            var session = GetSession();
            var service = new StorageService();

            WriteVerbose($"Allocating disk '{Filename}' ({Size}) on storage '{Storage}' on node '{Node}'...");
            var task = service.AllocateDisk(session, Node, Storage, config);

            WriteObject(task.Upid ?? string.Empty);
        }
    }
}
