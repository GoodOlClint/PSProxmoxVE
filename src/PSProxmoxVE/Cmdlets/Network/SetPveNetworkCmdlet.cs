using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Updates a network interface configuration on a Proxmox VE node.</para>
    /// <para type="description">
    /// Modifies the specified network interface on the node. After modifying, use
    /// Invoke-PveNetworkApply to apply pending changes to the running system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveNetwork", SupportsShouldProcess = true)]
    public class SetPveNetworkCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Node { get; set; } = string.Empty;

        /// <summary>The interface name to modify.</summary>
        [Parameter(Mandatory = true, Position = 1)]
        public string Iface { get; set; } = string.Empty;

        /// <summary>IPv4 address for the interface.</summary>
        [Parameter(Mandatory = false)]
        public string? Address { get; set; }

        /// <summary>IPv4 subnet mask.</summary>
        [Parameter(Mandatory = false)]
        public string? Netmask { get; set; }

        /// <summary>IPv4 gateway address.</summary>
        [Parameter(Mandatory = false)]
        public string? Gateway { get; set; }

        /// <summary>IPv6 address for the interface.</summary>
        [Parameter(Mandatory = false)]
        public string? Address6 { get; set; }

        /// <summary>IPv6 prefix length.</summary>
        [Parameter(Mandatory = false)]
        public int? Netmask6 { get; set; }

        /// <summary>IPv6 gateway address.</summary>
        [Parameter(Mandatory = false)]
        public string? Gateway6 { get; set; }

        /// <summary>Bridge ports (space-separated interface names).</summary>
        [Parameter(Mandatory = false)]
        public string? BridgePorts { get; set; }

        /// <summary>Bond slave interfaces (space-separated names).</summary>
        [Parameter(Mandatory = false)]
        public string? BondSlaves { get; set; }

        /// <summary>MTU override.</summary>
        [Parameter(Mandatory = false)]
        public int? Mtu { get; set; }

        /// <summary>Configure this interface to start automatically at boot.</summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Autostart { get; set; }

        /// <summary>Optional comments/notes for this interface.</summary>
        [Parameter(Mandatory = false)]
        public string? Comments { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"{Iface} on {Node}", "Set PVE Network Iface"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            var data = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(Address))     data["address"]      = Address!;
            if (!string.IsNullOrEmpty(Netmask))     data["netmask"]      = Netmask!;
            if (!string.IsNullOrEmpty(Gateway))     data["gateway"]      = Gateway!;
            if (!string.IsNullOrEmpty(Address6))    data["address6"]     = Address6!;
            if (Netmask6.HasValue)                  data["netmask6"]     = Netmask6.Value.ToString();
            if (!string.IsNullOrEmpty(Gateway6))    data["gateway6"]     = Gateway6!;
            if (!string.IsNullOrEmpty(BridgePorts)) data["bridge_ports"] = BridgePorts!;
            if (!string.IsNullOrEmpty(BondSlaves))  data["slaves"]       = BondSlaves!;
            if (Mtu.HasValue)                       data["mtu"]          = Mtu.Value.ToString();
            if (Autostart.IsPresent)                data["autostart"]    = "1";
            if (!string.IsNullOrEmpty(Comments))    data["comments"]     = Comments!;

            client.PutAsync($"nodes/{Node}/network/{Iface}", data).GetAwaiter().GetResult();
        }
    }
}
