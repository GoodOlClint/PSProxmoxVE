using System.Management.Automation;
using PSProxmoxVE.Core.Models.Network;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Lists network interfaces configured on a Proxmox VE node.</para>
    /// <para type="description">
    /// Returns network interface definitions from the specified node.
    /// Optionally filter by interface type. Node accepts pipeline input from Get-PveNode.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveNetwork")]
    [OutputType(typeof(PveNetwork))]
    public sealed class GetPveNetworkCmdlet : PveCmdletBase
    {
        /// <summary>
        /// The Proxmox VE node name. Accepts pipeline input from Get-PveNode (PveNode.Name).
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        [Alias("NodeName")]
        public string Node { get; set; } = string.Empty;

        /// <summary>Filter results to this specific interface name (e.g., "vmbr0").</summary>
        [Parameter(Mandatory = false, HelpMessage = "The network interface name.")]
        public string? Iface { get; set; }

        /// <summary>Filter by interface type (e.g., "bridge", "bond", "eth", "vlan").</summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by interface type (e.g. bridge, bond).")]
        [ValidateSet("bridge", "bond", "eth", "alias", "vlan", "OVSBridge", "OVSBond",
                     "OVSPort", "OVSIntPort", "any_bridge", "any_local_bridge", IgnoreCase = true)]
        public string? Type { get; set; }

        protected override void ProcessPveRecord()
        {
            var session = GetSession();

            WriteVerbose($"Getting network interfaces on node '{Node}'...");
            var service = new NetworkService();
            var networks = service.GetNetworks(session, Node, Type);

            foreach (var network in networks)
            {
                network.Node = Node;
                if (!string.IsNullOrEmpty(Iface) &&
                    !string.Equals(network.Iface, Iface, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                WriteObject(network);
            }
        }
    }
}
