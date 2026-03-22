using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Network;

/// <summary>
/// Represents a Software-Defined Networking (SDN) zone as returned by
/// the /cluster/sdn/zones endpoint.
/// Available in Proxmox VE 8.0+.
/// </summary>
public class PveSdnZone
{
    /// <summary>
    /// The unique zone identifier.
    /// </summary>
    [JsonProperty("zone")]
    public string Zone { get; set; } = string.Empty;

    /// <summary>
    /// The zone type (e.g., "simple", "vlan", "qinq", "vxlan", "evpn").
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The DNS server address associated with this zone.
    /// </summary>
    [JsonProperty("dns")]
    public string? Dns { get; set; }

    /// <summary>
    /// The reverse DNS zone name for this SDN zone.
    /// </summary>
    [JsonProperty("reversedns")]
    public string? Reversedns { get; set; }

    /// <summary>
    /// The DNS zone name used for forward lookups.
    /// </summary>
    [JsonProperty("dnszone")]
    public string? DnsZone { get; set; }

    /// <summary>
    /// The IPAM plugin to use for IP address management in this zone.
    /// </summary>
    [JsonProperty("ipam")]
    public string? Ipam { get; set; }

    /// <summary>
    /// The Maximum Transmission Unit in bytes for this zone.
    /// </summary>
    [JsonProperty("mtu")]
    public int? Mtu { get; set; }

    /// <summary>
    /// Comma-separated list of nodes that participate in this SDN zone.
    /// </summary>
    [JsonProperty("nodes")]
    public string? Nodes { get; set; }

    /// <summary>
    /// Indicates whether the zone has pending (unapplied) configuration changes (1) or not (0).
    /// </summary>
    [JsonProperty("pending")]
    public int? Pending { get; set; }

    /// <summary>
    /// Optional comment or description for this SDN zone.
    /// </summary>
    [JsonProperty("comments")]
    public string? Comments { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var pendingStr = Pending is 1 ? " [pending]" : string.Empty;
        return $"SDN Zone: {Zone}{pendingStr} | Type: {Type ?? "N/A"} | DNS: {Dns ?? "N/A"} | "
             + $"IPAM: {Ipam ?? "N/A"} | Nodes: {Nodes ?? "all"}";
    }
}
