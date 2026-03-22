using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Network;

/// <summary>
/// Represents a Software-Defined Networking (SDN) DNS plugin as returned by
/// the /cluster/sdn/dns endpoint. Requires Proxmox VE 8.0+.
/// </summary>
public class PveSdnDns
{
    /// <summary>
    /// The DNS plugin identifier.
    /// </summary>
    [JsonProperty("dns")]
    public string Dns { get; set; } = string.Empty;

    /// <summary>
    /// The DNS plugin type (e.g. "powerdns").
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the DNS service API.
    /// </summary>
    [JsonProperty("url")]
    public string? Url { get; set; }

    /// <summary>
    /// The API key for the DNS service.
    /// </summary>
    [JsonProperty("key")]
    public string? Key { get; set; }

    /// <summary>
    /// The IPv6 reverse zone mask length.
    /// </summary>
    [JsonProperty("reversemaskv6")]
    public int? ReverseMaskV6 { get; set; }

    /// <summary>
    /// The TTL (time-to-live) for DNS records.
    /// </summary>
    [JsonProperty("ttl")]
    public int? Ttl { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"SDN DNS: {Dns} | Type: {Type} | URL: {Url ?? "N/A"}";
    }
}
