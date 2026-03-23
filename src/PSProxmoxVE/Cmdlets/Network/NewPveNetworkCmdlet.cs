using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Creates a new network interface on a Proxmox VE node.</para>
    /// <para type="description">
    /// Adds a network interface definition to the specified node. After creation, use
    /// Invoke-PveNetworkApply to apply pending changes to the running system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveNetwork", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class NewPveNetworkCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The interface name (e.g., "vmbr1", "bond0").</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The network interface name.")]
        public string Iface { get; set; } = string.Empty;

        /// <summary>The interface type.</summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "The interface type (e.g. bridge, bond, vlan).")]
        [ValidateSet("bridge", "bond", "eth", "alias", "vlan", "OVSBridge", "OVSBond",
                     "OVSPort", "OVSIntPort", IgnoreCase = true)]
        public string Type { get; set; } = string.Empty;

        /// <summary>IPv4 address for the interface.</summary>
        [Parameter(Mandatory = false, HelpMessage = "IPv4 address for the interface.")]
        public string? Address { get; set; }

        /// <summary>IPv4 subnet mask.</summary>
        [Parameter(Mandatory = false, HelpMessage = "IPv4 subnet mask.")]
        public string? Netmask { get; set; }

        /// <summary>IPv4 gateway address.</summary>
        [Parameter(Mandatory = false, HelpMessage = "IPv4 gateway address.")]
        public string? Gateway { get; set; }

        /// <summary>Bridge ports (space-separated interface names, for bridge type).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Bridge ports (space-separated interface names).")]
        public string? BridgePorts { get; set; }

        /// <summary>Bond slave interfaces (space-separated names, for bond type).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Bond slave interfaces (space-separated names).")]
        public string? BondSlaves { get; set; }

        /// <summary>VLAN tag ID (for vlan type).</summary>
        [Parameter(Mandatory = false, HelpMessage = "VLAN tag ID.")]
        public int? VlanId { get; set; }

        /// <summary>MTU override.</summary>
        [Parameter(Mandatory = false, HelpMessage = "MTU override.")]
        public int? Mtu { get; set; }

        /// <summary>Configure this interface to start automatically at boot.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Start interface automatically at boot.")]
        public SwitchParameter Autostart { get; set; }

        /// <summary>Optional comments/notes for this interface.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comments or notes for this interface.")]
        public string? Comments { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"{Iface} on {Node}", "Create PVE Network Iface"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            WriteVerbose($"Creating network interface '{Iface}' on node '{Node}'...");
            var data = new Dictionary<string, string>
            {
                ["iface"] = Iface,
                ["type"]  = Type
            };

            if (!string.IsNullOrEmpty(Address))     data["address"]      = Address!;
            if (!string.IsNullOrEmpty(Netmask))     data["netmask"]      = Netmask!;
            if (!string.IsNullOrEmpty(Gateway))     data["gateway"]      = Gateway!;
            if (!string.IsNullOrEmpty(BridgePorts)) data["bridge_ports"] = BridgePorts!;
            if (!string.IsNullOrEmpty(BondSlaves))  data["slaves"]       = BondSlaves!;
            if (VlanId.HasValue)                    data["vlan-id"]      = VlanId.Value.ToString();
            if (Mtu.HasValue)                       data["mtu"]          = Mtu.Value.ToString();
            if (Autostart.IsPresent)                data["autostart"]    = "1";
            if (!string.IsNullOrEmpty(Comments))    data["comments"]     = Comments!;

            client.PostAsync($"nodes/{Uri.EscapeDataString(Node)}/network", data).GetAwaiter().GetResult();
        }
    }
}
