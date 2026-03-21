using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Network;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Lists SDN zones defined in Proxmox VE.</para>
    /// <para type="description">
    /// Returns Software-Defined Networking zone definitions from the cluster SDN configuration.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveSdnZone")]
    [OutputType(typeof(PveSdnZone))]
    public class GetPveSdnZoneCmdlet : PveCmdletBase
    {
        /// <summary>Optional zone identifier to retrieve a specific zone.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "The SDN zone name.")]
        public string? Zone { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);
            using var client = new PveHttpClient(session);

            WriteVerbose("Getting SDN zones...");
            var resource = "cluster/sdn/zones";
            var json = client.GetAsync(resource).GetAwaiter().GetResult();
            var root = JObject.Parse(json);
            var data = root["data"] as JArray ?? new JArray();

            foreach (var item in data)
            {
                var zone = item.ToObject<PveSdnZone>()!;
                if (!string.IsNullOrEmpty(Zone) &&
                    !string.Equals(zone.Zone, Zone, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                WriteObject(zone);
            }
        }
    }
}
