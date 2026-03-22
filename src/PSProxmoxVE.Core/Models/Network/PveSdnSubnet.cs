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
    [JsonProperty("subnet")]
    public string Subnet { get; set; } = string.Empty;

    /// <summary>
    /// The VNet this subnet belongs to.
    /// </summary>
    [JsonProperty("vnet")]
    public string? Vnet { get; set; }

    /// <summary>
    /// The gateway IP address for this subnet.
    /// </summary>
    [JsonProperty("gateway")]
    public string? Gateway { get; set; }

    /// <summary>
    /// Whether SNAT (source NAT) is enabled for this subnet.
    /// </summary>
    [JsonProperty("snat")]
    public int? Snat { get; set; }

    /// <summary>
    /// The DNS zone name for this subnet.
    /// </summary>
    [JsonProperty("dnszoneprefix")]
    public string? DnsZonePrefix { get; set; }

    /// <summary>
    /// The DHCP range configurations for automatic IP assignment.
    /// </summary>
    [JsonProperty("dhcp-range")]
    public string[]? DhcpRanges { get; set; }

    /// <summary>
    /// The subnet type identifier used internally by PVE.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Optional comment or description.
    /// </summary>
    [JsonProperty("comments")]
    public string? Comments { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"SDN Subnet: {Subnet} | VNet: {Vnet ?? "N/A"} | "
             + $"Gateway: {Gateway ?? "N/A"}";
    }
}
