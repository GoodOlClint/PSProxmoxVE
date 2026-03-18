using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Network;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Lists SDN VNets defined in Proxmox VE.</para>
    /// <para type="description">
    /// Returns Software-Defined Networking VNet definitions from the cluster SDN configuration.
    /// Optionally filter by zone.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveSdnVnet")]
    [OutputType(typeof(PveSdnVnet))]
    public class GetPveSdnVnetCmdlet : PveCmdletBase
    {
        /// <summary>Filter VNets to a specific zone.</summary>
        [Parameter(Mandatory = false, Position = 0)]
        public string? Zone { get; set; }

        /// <summary>Optional VNet identifier to retrieve a specific VNet.</summary>
        [Parameter(Mandatory = false)]
        public string? Vnet { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            using var client = new PveHttpClient(session);

            var json = client.GetAsync("cluster/sdn/vnets").GetAwaiter().GetResult();
            var root = JObject.Parse(json);
            var data = root["data"] as JArray ?? new JArray();

            foreach (var item in data)
            {
                var vnet = item.ToObject<PveSdnVnet>()!;

                if (!string.IsNullOrEmpty(Zone) &&
                    !string.Equals(vnet.Zone, Zone, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(Vnet) &&
                    !string.Equals(vnet.Vnet, Vnet, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteObject(vnet);
            }
        }
    }
}
