using System.Management.Automation;
using PSProxmoxVE.Core.Models.Nodes;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Returns detailed status information for a Proxmox VE node.</para>
    /// <para type="description">
    /// Get-PveNodeStatus retrieves CPU, memory, disk, swap, network and uptime statistics
    /// for the specified node from the /nodes/{node}/status endpoint.
    /// The -Node parameter accepts values from the pipeline by property name, so you can
    /// pipe output from Get-PveNode directly.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveNodeStatus")]
    [OutputType(typeof(PveNodeStatus))]
    public sealed class GetPveNodeStatusCmdlet : PveCmdletBase
    {
        /// <summary>
        /// Name of the node to query. Accepts pipeline input via the PveNode.Name property.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        protected override void ProcessPveRecord()
        {
            var session = GetSession();

            WriteVerbose($"Getting status for node '{Node}'...");
            var service = new NodeService();
            var status = service.GetNodeStatus(session, Node);

            WriteObject(status);
        }
    }
}
