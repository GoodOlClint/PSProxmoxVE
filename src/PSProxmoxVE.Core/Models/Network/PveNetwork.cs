using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Network;

/// <summary>
/// Represents a network interface configuration on a Proxmox VE node,
/// as returned by the /nodes/{node}/network endpoint.
/// </summary>
public class PveNetwork
{
    /// <summary>
    /// The interface name (e.g., "vmbr0", "eth0", "bond0").
    /// </summary>
    [JsonPropertyName("iface")]
    [JsonProperty("iface")]
    public string Iface { get; set; } = string.Empty;

    /// <summary>
    /// The interface type (e.g., "bridge", "bond", "eth", "vlan", "OVSBridge").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The IPv4 address assigned to this interface.
    /// </summary>
    [JsonPropertyName("address")]
    [JsonProperty("address")]
    public string? Address { get; set; }

    /// <summary>
    /// The IPv4 netmask for this interface.
    /// </summary>
    [JsonPropertyName("netmask")]
    [JsonProperty("netmask")]
    public string? Netmask { get; set; }

    /// <summary>
    /// The IPv4 default gateway associated with this interface.
    /// </summary>
    [JsonPropertyName("gateway")]
    [JsonProperty("gateway")]
    public string? Gateway { get; set; }

    /// <summary>
    /// Space-separated list of physical ports attached to this bridge.
    /// </summary>
    [JsonPropertyName("bridge_ports")]
    [JsonProperty("bridge_ports")]
    public string? BridgePorts { get; set; }

    /// <summary>
    /// Space-separated list of slave interfaces for bond/team interfaces.
    /// </summary>
    [JsonPropertyName("slaves")]
    [JsonProperty("slaves")]
    public string? BondSlaves { get; set; }

    /// <summary>
    /// VLAN ID for VLAN sub-interfaces.
    /// </summary>
    [JsonPropertyName("vlan-id")]
    [JsonProperty("vlan-id")]
    public int? VlanId { get; set; }

    /// <summary>
    /// The Maximum Transmission Unit in bytes.
    /// </summary>
    [JsonPropertyName("mtu")]
    [JsonProperty("mtu")]
    public int? Mtu { get; set; }

    /// <summary>
    /// Indicates whether the interface is brought up automatically at boot (1) or not (0).
    /// </summary>
    [JsonPropertyName("autostart")]
    [JsonProperty("autostart")]
    public int? Autostart { get; set; }

    /// <summary>
    /// Indicates whether the interface is currently active/up (1) or down (0).
    /// </summary>
    [JsonPropertyName("active")]
    [JsonProperty("active")]
    public int? Active { get; set; }

    /// <summary>
    /// Optional comment or description for this interface.
    /// </summary>
    [JsonPropertyName("comments")]
    [JsonProperty("comments")]
    public string? Comments { get; set; }

    /// <summary>
    /// IPv4 address in CIDR notation (e.g., "192.168.1.1/24").
    /// </summary>
    [JsonPropertyName("cidr")]
    [JsonProperty("cidr")]
    public string? Cidr { get; set; }

    /// <summary>
    /// Indicates whether VLAN-aware bridging is enabled on this bridge (1) or not (0).
    /// </summary>
    [JsonPropertyName("bridge_vlan_aware")]
    [JsonProperty("bridge_vlan_aware")]
    public int? BridgeVlanAware { get; set; }

    /// <summary>
    /// The bonding mode for bond interfaces (e.g., "active-backup", "802.3ad", "balance-rr").
    /// </summary>
    [JsonPropertyName("bond_mode")]
    [JsonProperty("bond_mode")]
    public string? BondMode { get; set; }

    /// <summary>
    /// The node this network interface belongs to. Populated by cmdlets after retrieval.
    /// </summary>
    public string? Node { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var addr = Cidr ?? Address ?? "N/A";
        var activeStr = Active is 1 ? "up" : "down";
        return $"Network: {Iface} | Type: {Type ?? "N/A"} | Address: {addr} | "
             + $"GW: {Gateway ?? "N/A"} | MTU: {Mtu?.ToString() ?? "N/A"} | {activeStr}";
    }
}
