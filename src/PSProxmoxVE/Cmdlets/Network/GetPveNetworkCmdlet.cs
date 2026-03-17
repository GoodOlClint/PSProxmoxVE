using System;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Network;

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
    public class GetPveNetworkCmdlet : PveCmdletBase
    {
        /// <summary>
        /// The Proxmox VE node name. Accepts pipeline input from Get-PveNode (PveNode.Name).
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("NodeName")]
        public string Node { get; set; } = string.Empty;

        /// <summary>Filter by interface type (e.g., "bridge", "bond", "eth", "vlan").</summary>
        [Parameter(Mandatory = false)]
        [ValidateSet("bridge", "bond", "eth", "alias", "vlan", "OVSBridge", "OVSBond",
                     "OVSPort", "OVSIntPort", "any_bridge", "any_local_bridge", IgnoreCase = true)]
        public string? Type { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            using var client = new PveHttpClient(session);

            var resource = $"/nodes/{Node}/network";
            if (!string.IsNullOrEmpty(Type))
                resource += $"?type={Uri.EscapeDataString(Type)}";

            var json = client.GetAsync(resource).GetAwaiter().GetResult();
            var root = JObject.Parse(json);
            var data = root["data"] as JArray ?? new JArray();

            foreach (var item in data)
            {
                var network = item.ToObject<PveNetwork>()!;
                network.Node = Node;
                WriteObject(network);
            }
        }
    }
}
