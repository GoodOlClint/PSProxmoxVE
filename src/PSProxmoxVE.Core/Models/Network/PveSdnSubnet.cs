using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Network;

/// <summary>
/// Represents a Software-Defined Networking (SDN) subnet as returned by
/// the /cluster/sdn/vnets/{vnet}/subnets endpoint.
/// Available in Proxmox VE 8.0+.
/// </summary>
public class PveSdnSubnet
{
    /// <summary>
    /// The subnet CIDR (e.g. "10.0.0.0/24" or "2001:db8::/64").
    /// </summary>
    [JsonPropertyName("subnet")]
    [JsonProperty("subnet")]
    public string Subnet { get; set; } = string.Empty;

    /// <summary>
    /// The VNet this subnet belongs to.
    /// </summary>
    [JsonPropertyName("vnet")]
    [JsonProperty("vnet")]
    public string? Vnet { get; set; }

    /// <summary>
    /// The gateway IP address for this subnet.
    /// </summary>
    [JsonPropertyName("gateway")]
    [JsonProperty("gateway")]
    public string? Gateway { get; set; }

    /// <summary>
    /// Whether SNAT (source NAT) is enabled for this subnet.
    /// </summary>
    [JsonPropertyName("snat")]
    [JsonProperty("snat")]
    public int? Snat { get; set; }

    /// <summary>
    /// The DNS zone name for this subnet.
    /// </summary>
    [JsonPropertyName("dnszoneprefix")]
    [JsonProperty("dnszoneprefix")]
    public string? DnsZonePrefix { get; set; }

    /// <summary>
    /// The DHCP range configuration for automatic IP assignment.
    /// </summary>
    [JsonPropertyName("dhcp-range")]
    [JsonProperty("dhcp-range")]
    public string? DhcpRange { get; set; }

    /// <summary>
    /// The subnet type identifier used internally by PVE.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Optional comment or description.
    /// </summary>
    [JsonPropertyName("comments")]
    [JsonProperty("comments")]
    public string? Comments { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"SDN Subnet: {Subnet} | VNet: {Vnet ?? "N/A"} | "
             + $"Gateway: {Gateway ?? "N/A"}";
    }
}
