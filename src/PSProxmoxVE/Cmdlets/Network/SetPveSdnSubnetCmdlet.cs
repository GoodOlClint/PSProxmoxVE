using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Updates an SDN subnet configuration in Proxmox VE.</para>
    /// <para type="description">
    /// Modifies the specified Software-Defined Networking subnet configuration on a VNet.
    /// Changes are pending until Invoke-PveSdnApply is called.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveSdnSubnet", SupportsShouldProcess = true)]
    public class SetPveSdnSubnetCmdlet : PveCmdletBase
    {
        /// <summary>The VNet this subnet belongs to.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The SDN VNet name.")]
        public string Vnet { get; set; } = string.Empty;

        /// <summary>The subnet CIDR.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The subnet CIDR.")]
        public string Subnet { get; set; } = string.Empty;

        /// <summary>Gateway address for this subnet.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Gateway address for this subnet.")]
        public string? Gateway { get; set; }

        /// <summary>Enable SNAT for this subnet.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Enable SNAT for this subnet.")]
        public SwitchParameter Snat { get; set; }

        /// <summary>DNS zone prefix for automatic registration.</summary>
        [Parameter(Mandatory = false, HelpMessage = "DNS zone prefix for automatic registration.")]
        public string? DnsZonePrefix { get; set; }

        /// <summary>DHCP range for this subnet.</summary>
        [Parameter(Mandatory = false, HelpMessage = "DHCP range for this subnet.")]
        public string? DhcpRange { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Subnet, "Set PVE SDN Subnet"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);
            var service = new NetworkService();

            WriteVerbose($"Updating SDN subnet '{Subnet}' on VNet '{Vnet}'...");
            var config = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(Gateway))      config["gateway"]        = Gateway!;
            if (MyInvocation.BoundParameters.ContainsKey("Snat"))
                config["snat"] = Snat.IsPresent ? "1" : "0";
            if (!string.IsNullOrEmpty(DnsZonePrefix)) config["dnszoneprefix"] = DnsZonePrefix!;
            if (!string.IsNullOrEmpty(DhcpRange))     config["dhcp-range"]    = DhcpRange!;

            service.UpdateSdnSubnet(session, Vnet, Subnet, config);
        }
    }
}
