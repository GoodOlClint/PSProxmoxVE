using System.Management.Automation;
using PSProxmoxVE.Core.Models.Network;
using PSProxmoxVE.Core.Services;

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
    public sealed class GetPveSdnVnetCmdlet : PveCmdletBase
    {
        /// <summary>Filter VNets to a specific zone.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "The SDN zone name.")]
        public string? Zone { get; set; }

        /// <summary>Optional VNet identifier to retrieve a specific VNet.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The SDN VNet name.")]
        public string? Vnet { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);

            WriteVerbose("Getting SDN VNets...");
            var service = new NetworkService();
            var vnets = service.GetSdnVnets(session);

            foreach (var vnet in vnets)
            {
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
