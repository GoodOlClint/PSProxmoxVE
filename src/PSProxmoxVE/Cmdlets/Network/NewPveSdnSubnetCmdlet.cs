using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Creates a new SDN subnet on a VNet in Proxmox VE.</para>
    /// <para type="description">
    /// Adds a new Software-Defined Networking subnet to the specified SDN VNet.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveSdnSubnet", SupportsShouldProcess = true)]
    public class NewPveSdnSubnetCmdlet : PveCmdletBase
    {
        /// <summary>The SDN VNet to add the subnet to.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The SDN VNet name.")]
        public string Vnet { get; set; } = string.Empty;

        /// <summary>The subnet CIDR notation (e.g. "10.0.0.0/24").</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The subnet in CIDR notation (e.g. 10.0.0.0/24).")]
        public string Subnet { get; set; } = string.Empty;

        /// <summary>The gateway IP address for this subnet.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Gateway IP address for the subnet.")]
        public string? Gateway { get; set; }

        /// <summary>Enable SNAT (source NAT) for this subnet.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Enable source NAT for this subnet.")]
        public SwitchParameter Snat { get; set; }

        /// <summary>DNS zone prefix for this subnet.</summary>
        [Parameter(Mandatory = false, HelpMessage = "DNS zone prefix for this subnet.")]
        public string? DnsZonePrefix { get; set; }

        /// <summary>DHCP range for automatic IP assignment (e.g. "start-address=10.0.0.100,end-address=10.0.0.200").</summary>
        [Parameter(Mandatory = false, HelpMessage = "DHCP range for automatic IP assignment.")]
        public string? DhcpRange { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Subnet {Subnet} on VNet {Vnet}", "Create PVE SDN Subnet"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);

            if (!string.IsNullOrEmpty(DhcpRange)
                && session.ServerVersion != null && !session.ServerVersion.IsAtLeast(8, 1))
            {
                WriteWarning("The -DhcpRange parameter requires PVE 8.1 or later. "
                    + $"Connected server is PVE {session.ServerVersion}. The parameter will be sent but may be ignored.");
            }

            using var client = new PveHttpClient(session);

            WriteVerbose($"Creating SDN subnet '{Subnet}' on VNet '{Vnet}'...");
            var data = new Dictionary<string, string>
            {
                ["subnet"] = Subnet,
                ["type"] = "subnet"
            };

            if (!string.IsNullOrEmpty(Gateway))       data["gateway"]        = Gateway!;
            if (Snat.IsPresent)                        data["snat"]           = "1";
            if (!string.IsNullOrEmpty(DnsZonePrefix))  data["dnszoneprefix"]  = DnsZonePrefix!;
            if (!string.IsNullOrEmpty(DhcpRange))      data["dhcp-range"]     = DhcpRange!;

            client.PostAsync(
                $"cluster/sdn/vnets/{System.Uri.EscapeDataString(Vnet)}/subnets", data)
                .GetAwaiter().GetResult();
        }
    }
}
