using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Updates properties of a content volume in Proxmox VE storage.</para>
    /// <para type="description">
    /// Modifies metadata (such as notes/description) of an existing volume in the specified
    /// storage on the given node.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveStorageContent", SupportsShouldProcess = true)]
    public class SetPveStorageContentCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        /// <summary>The storage identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The storage pool name.")]
        [ValidateNotNullOrEmpty]
        public string Storage { get; set; } = string.Empty;

        /// <summary>The volume identifier to update.</summary>
        [Parameter(Mandatory = true, Position = 2, ValueFromPipelineByPropertyName = true, HelpMessage = "The volume identifier to update.")]
        [ValidateNotNullOrEmpty]
        public string Volume { get; set; } = string.Empty;

        /// <summary>Notes/description to set on the volume.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Notes or description for the volume.")]
        public string? Notes { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Volume, "Set PVE Storage Content"))
                return;

            var config = new Dictionary<string, string>();
            if (Notes != null) config["notes"] = Notes;

            var session = GetSession();
            var service = new StorageService();

            WriteVerbose($"Updating volume '{Volume}' on storage '{Storage}' on node '{Node}'...");
            service.UpdateContent(session, Node, Storage, Volume, config);
        }
    }
}
