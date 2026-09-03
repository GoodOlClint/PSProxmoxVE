using System.Management.Automation;
using PSProxmoxVE.Core.Models.Network;
using PSProxmoxVE.Core.Services;

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
    public sealed class GetPveSdnZoneCmdlet : PveCmdletBase
    {
        /// <summary>Optional zone identifier to retrieve a specific zone.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "The SDN zone name.")]
        public string? Zone { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);

            WriteVerbose("Getting SDN zones...");
            var service = new NetworkService();
            var zones = service.GetSdnZones(session);

            foreach (var zone in zones)
            {
                if (!string.IsNullOrEmpty(Zone) &&
                    !string.Equals(zone.Zone, Zone, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                WriteObject(zone);
            }
        }
    }
}
