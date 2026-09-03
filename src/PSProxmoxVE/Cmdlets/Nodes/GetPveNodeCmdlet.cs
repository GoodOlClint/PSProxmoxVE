using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Nodes;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Returns one or more Proxmox VE cluster nodes.</para>
    /// <para type="description">
    /// Get-PveNode retrieves all nodes visible to the authenticated session.
    /// Use -Name to filter to a specific node by exact name.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveNode")]
    [Alias("gpn")]
    [OutputType(typeof(PveNode))]
    public sealed class GetPveNodeCmdlet : PveCmdletBase
    {
        /// <summary>Optional node name filter. When specified, only the matching node is returned.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "Filter by node name.")]
        [ValidateNotNullOrEmpty]
        public string? Name { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose("Getting cluster nodes...");
            var service = new NodeService();
            var nodes = service.GetNodes(session);

            foreach (var node in nodes)
            {
                if (Name is null || string.Equals(node.Name, Name, StringComparison.OrdinalIgnoreCase))
                    WriteObject(node);
            }
        }
    }
}
