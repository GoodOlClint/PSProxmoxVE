using System.Management.Automation;
using PSProxmoxVE.Core.Models.Storage;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Returns the status of a Proxmox VE storage on a node.</para>
    /// <para type="description">
    /// Retrieves capacity, usage and activation status for the specified storage
    /// on the given node from the /nodes/{node}/storage/{storage}/status endpoint.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveStorageStatus")]
    [OutputType(typeof(PveStorageStatus))]
    public sealed class GetPveStorageStatusCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        /// <summary>The storage identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The storage pool name.")]
        [ValidateNotNullOrEmpty]
        public string Storage { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new StorageService();

            WriteVerbose($"Getting status for storage '{Storage}' on node '{Node}'...");
            var status = service.GetStorageStatus(session, Node, Storage);
            status.Storage = Storage;
            status.Node = Node;

            WriteObject(status);
        }
    }
}
