using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Network;

/// <summary>
/// Represents a Software-Defined Networking (SDN) virtual network (VNet) as returned by
/// the /cluster/sdn/vnets endpoint.
/// Available in Proxmox VE 8.0+.
/// </summary>
public class PveSdnVnet
{
    /// <summary>
    /// The unique VNet identifier.
    /// </summary>
    [JsonPropertyName("vnet")]
    [JsonProperty("vnet")]
    public string Vnet { get; set; } = string.Empty;

    /// <summary>
    /// The SDN zone this VNet belongs to.
    /// </summary>
    [JsonPropertyName("zone")]
    [JsonProperty("zone")]
    public string? Zone { get; set; }

    /// <summary>
    /// The VLAN or VXLAN tag associated with this VNet.
    /// </summary>
    [JsonPropertyName("tag")]
    [JsonProperty("tag")]
    public int? Tag { get; set; }

    /// <summary>
    /// An optional human-readable alias for this VNet.
    /// </summary>
    [JsonPropertyName("alias")]
    [JsonProperty("alias")]
    public string? Alias { get; set; }

    /// <summary>
    /// Indicates whether VLAN-aware mode is enabled on this VNet (1) or not (0).
    /// </summary>
    [JsonPropertyName("vlanaware")]
    [JsonProperty("vlanaware")]
    public int? VlanAware { get; set; }

    /// <summary>
    /// Optional comment or description for this SDN VNet.
    /// </summary>
    [JsonPropertyName("comments")]
    [JsonProperty("comments")]
    public string? Comments { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var vlanAwareStr = VlanAware is 1 ? " [vlan-aware]" : string.Empty;
        return $"SDN VNet: {Vnet}{vlanAwareStr} | Zone: {Zone ?? "N/A"} | "
             + $"Tag: {Tag?.ToString() ?? "N/A"} | Alias: {Alias ?? "N/A"}";
    }
}
