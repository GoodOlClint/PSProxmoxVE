using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Removes a content volume from a Proxmox VE storage.</para>
    /// <para type="description">
    /// Deletes the specified volume (disk image, ISO, template, backup, etc.) from the
    /// storage on the given node.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveStorageContent",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveStorageContentCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        /// <summary>The storage identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The storage pool name.")]
        [ValidateNotNullOrEmpty]
        public string Storage { get; set; } = string.Empty;

        /// <summary>The volume identifier to remove (e.g. "local:iso/ubuntu.iso").</summary>
        [Parameter(Mandatory = true, Position = 2, ValueFromPipelineByPropertyName = true, HelpMessage = "The volume identifier to remove.")]
        [ValidateNotNullOrEmpty]
        public string Volume { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Volume, "Remove PVE Storage Content"))
                return;

            var session = GetSession();
            var service = new StorageService();

            WriteVerbose($"Removing volume '{Volume}' from storage '{Storage}' on node '{Node}'...");
            service.RemoveContent(session, Node, Storage, Volume);
        }
    }
}
