using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Storage;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Lists storage definitions on a Proxmox VE node or cluster.</para>
    /// <para type="description">
    /// Returns storage definitions visible to the specified node. When Node is omitted the
    /// cluster-level storage list is queried. Results can be filtered by Type and ContentType.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveStorage")]
    [Alias("gpvs")]
    [OutputType(typeof(PveStorage))]
    public class GetPveStorageCmdlet : PveCmdletBase
    {
        /// <summary>
        /// The Proxmox VE node name. Accepts pipeline input from Get-PveNode (PveNode.Name).
        /// When omitted the cluster-wide storage list is used.
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        [Alias("NodeName")]
        public string? Node { get; set; }

        /// <summary>Filter results to a specific storage name (e.g., "local", "local-lvm").</summary>
        [Parameter(Mandatory = false, HelpMessage = "The storage pool name.")]
        public string? Storage { get; set; }

        /// <summary>Filter results to a specific storage type (e.g., "dir", "nfs", "zfspool").</summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by storage type (e.g. dir, nfs, zfspool).")]
        public string? Type { get; set; }

        /// <summary>Filter results to storages that support the given content type (e.g., "iso", "backup").</summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by content type (e.g. iso, backup).")]
        public string? ContentType { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose("Getting storage pools...");
            var service = new StorageService();

            var storages = service.GetStorages(session, Node);

            foreach (var storage in storages)
            {
                if (!string.IsNullOrEmpty(Node))
                    storage.Node = Node;

                if (!string.IsNullOrEmpty(Storage) &&
                    !string.Equals(storage.Storage, Storage, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(Type) &&
                    !string.Equals(storage.Type, Type, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(ContentType) && storage.Content != null &&
                    !storage.Content.Contains(ContentType))
                    continue;

                WriteObject(storage);
            }
        }
    }
}
