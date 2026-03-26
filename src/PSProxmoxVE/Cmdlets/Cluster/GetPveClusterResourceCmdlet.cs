using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Cluster;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Lists resources across the Proxmox VE cluster.</para>
    /// <para type="description">
    /// Returns cluster-wide resources (VMs, containers, nodes, storage, SDN).
    /// Optionally filter by resource type or node name.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveClusterResource")]
    [OutputType(typeof(PveClusterResource))]
    public sealed class GetPveClusterResourceCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">Filter by resource type.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "Filter by resource type (vm, node, storage, sdn).")]
        [ValidateSet("vm", "node", "storage", "sdn")]
        public string? Type { get; set; }

        /// <summary>
        /// <para type="description">Filter results to a specific node name.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter results to a specific node name.")]
        public string? Node { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new ClusterService();

            WriteVerbose($"Getting cluster resources{(Type != null ? $" of type '{Type}'" : "")}...");
            var resources = service.GetClusterResources(session, Type);

            foreach (var resource in resources)
            {
                if (!string.IsNullOrEmpty(Node) &&
                    !string.Equals(resource.Node, Node, StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteObject(resource);
            }
        }
    }
}
