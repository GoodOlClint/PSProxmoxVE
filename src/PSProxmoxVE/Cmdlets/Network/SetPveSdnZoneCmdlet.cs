using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Updates an SDN zone configuration in Proxmox VE.</para>
    /// <para type="description">
    /// Modifies the specified Software-Defined Networking zone configuration.
    /// Changes are pending until Invoke-PveSdnApply is called.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveSdnZone", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveSdnZoneCmdlet : PveCmdletBase
    {
        /// <summary>The zone identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The SDN zone name.")]
        public string Zone { get; set; } = string.Empty;

        /// <summary>DNS server for automatic DNS registration.</summary>
        [Parameter(Mandatory = false, HelpMessage = "DNS server for automatic registration.")]
        public string? Dns { get; set; }

        /// <summary>Reverse DNS server.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Reverse DNS server.")]
        public string? Reversedns { get; set; }

        /// <summary>DNS zone name for registration.</summary>
        [Parameter(Mandatory = false, HelpMessage = "DNS zone name for registration.")]
        public string? DnsZone { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Zone, "Set PVE SDN Zone"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);
            var service = new NetworkService();

            WriteVerbose($"Updating SDN zone '{Zone}'...");
            var config = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(Dns))        config["dns"]        = Dns!;
            if (!string.IsNullOrEmpty(Reversedns)) config["reversedns"] = Reversedns!;
            if (!string.IsNullOrEmpty(DnsZone))    config["dnszone"]    = DnsZone!;

            service.UpdateSdnZone(session, Zone, config);
        }
    }
}
