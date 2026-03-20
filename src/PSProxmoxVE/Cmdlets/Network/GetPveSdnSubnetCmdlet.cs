using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Network;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Lists SDN subnets for a VNet in Proxmox VE.</para>
    /// <para type="description">
    /// Returns Software-Defined Networking subnet definitions for the specified VNet.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveSdnSubnet")]
    [OutputType(typeof(PveSdnSubnet))]
    public class GetPveSdnSubnetCmdlet : PveCmdletBase
    {
        /// <summary>The SDN VNet to list subnets for.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The SDN VNet name.")]
        public string Vnet { get; set; } = string.Empty;

        /// <summary>Optional subnet CIDR filter.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by subnet CIDR (e.g. 10.0.0.0/24).")]
        public string? Subnet { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            using var client = new PveHttpClient(session);

            WriteVerbose($"Getting SDN subnets for VNet '{Vnet}'...");
            var json = client.GetAsync($"cluster/sdn/vnets/{System.Uri.EscapeDataString(Vnet)}/subnets")
                .GetAwaiter().GetResult();
            var root = JObject.Parse(json);
            var data = root["data"] as JArray ?? new JArray();

            foreach (var item in data)
            {
                var subnet = item.ToObject<PveSdnSubnet>()!;

                if (!string.IsNullOrEmpty(Subnet) &&
                    !string.Equals(subnet.Subnet, Subnet, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteObject(subnet);
            }
        }
    }
}
