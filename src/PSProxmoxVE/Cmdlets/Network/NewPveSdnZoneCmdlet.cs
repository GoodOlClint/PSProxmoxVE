using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Creates a new SDN zone in Proxmox VE.</para>
    /// <para type="description">
    /// Adds a new Software-Defined Networking zone to the Proxmox VE cluster configuration.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveSdnZone", SupportsShouldProcess = true)]
    public class NewPveSdnZoneCmdlet : PveCmdletBase
    {
        /// <summary>The zone identifier (alphanumeric, hyphens allowed).</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Zone { get; set; } = string.Empty;

        /// <summary>The zone type.</summary>
        [Parameter(Mandatory = true, Position = 1)]
        [ValidateSet("vlan", "vxlan", "evpn", "simple", "qinq", IgnoreCase = true)]
        public string Type { get; set; } = string.Empty;

        /// <summary>VXLAN peer list or multicast address (for vxlan/evpn types).</summary>
        [Parameter(Mandatory = false)]
        public string? Peers { get; set; }

        /// <summary>Bridge interface this zone attaches to (for vlan/qinq types).</summary>
        [Parameter(Mandatory = false)]
        public string? Bridge { get; set; }

        /// <summary>MTU override for this zone.</summary>
        [Parameter(Mandatory = false)]
        public int? Mtu { get; set; }

        /// <summary>DNS server for automatic DNS registration.</summary>
        [Parameter(Mandatory = false)]
        public string? Dns { get; set; }

        /// <summary>Reverse DNS server.</summary>
        [Parameter(Mandatory = false)]
        public string? ReverseDns { get; set; }

        /// <summary>DNS zone name for registration.</summary>
        [Parameter(Mandatory = false)]
        public string? DnsZone { get; set; }

        /// <summary>IPAM plugin to use for this zone.</summary>
        [Parameter(Mandatory = false)]
        public string? Ipam { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Zone, "Create PVE SDN Zone"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            var data = new Dictionary<string, string>
            {
                ["zone"] = Zone,
                ["type"] = Type
            };

            if (!string.IsNullOrEmpty(Peers))      data["peers"]      = Peers;
            if (!string.IsNullOrEmpty(Bridge))     data["bridge"]     = Bridge;
            if (Mtu.HasValue)                      data["mtu"]        = Mtu.Value.ToString();
            if (!string.IsNullOrEmpty(Dns))        data["dns"]        = Dns;
            if (!string.IsNullOrEmpty(ReverseDns)) data["reversedns"] = ReverseDns;
            if (!string.IsNullOrEmpty(DnsZone))    data["dnszone"]    = DnsZone;
            if (!string.IsNullOrEmpty(Ipam))       data["ipam"]       = Ipam;

            client.PostAsync("cluster/sdn/zones", data).GetAwaiter().GetResult();
        }
    }
}
