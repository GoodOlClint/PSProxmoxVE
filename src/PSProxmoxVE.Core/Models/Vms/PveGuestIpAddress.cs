using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents an IP address reported by the QEMU guest agent.
/// </summary>
public class PveGuestIpAddress
{
    /// <summary>The IP address string (e.g., "192.168.1.100" or "fe80::1").</summary>
    [JsonPropertyName("ip-address")]
    [JsonProperty("ip-address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>The address type: "ipv4" or "ipv6".</summary>
    [JsonPropertyName("ip-address-type")]
    [JsonProperty("ip-address-type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>The network prefix length (e.g., 24 for a /24 subnet).</summary>
    [JsonPropertyName("prefix")]
    [JsonProperty("prefix")]
    public int Prefix { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{Address}/{Prefix} ({Type})";
}
