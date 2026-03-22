using System.Management.Automation;
using PSProxmoxVE.Core.Models.Storage;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Lists content items within a Proxmox VE storage.</para>
    /// <para type="description">
    /// Returns volume entries (ISOs, templates, backups, disk images) stored in the
    /// specified storage on the given node. Optionally filter by content type.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveStorageContent")]
    [OutputType(typeof(PveStorageContent))]
    public sealed class GetPveStorageContentCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name that hosts the storage.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// The storage identifier. Accepts pipeline input from Get-PveStorage (PveStorage.Storage).
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The storage pool name.")]
        public string Storage { get; set; } = string.Empty;

        /// <summary>Filter results to a specific content type (e.g., "iso", "vztmpl", "backup", "images").</summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by content type (e.g. iso, vztmpl, backup).")]
        public string? ContentType { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose($"Getting content for storage '{Storage}' on node '{Node}'...");
            var service = new StorageService();

            var items = service.GetStorageContent(session, Node, Storage, ContentType);

            foreach (var content in items)
            {
                content.Storage = Storage;
                content.Node    = Node;
                WriteObject(content);
            }
        }
    }
}
