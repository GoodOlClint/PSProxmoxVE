using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Firewall;

/// <summary>
/// Represents a Proxmox VE firewall IP set as returned by
/// the firewall/ipset endpoints.
/// </summary>
public class PveFirewallIpSet
{
    /// <summary>
    /// The IP set name.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional comment describing the IP set.
    /// </summary>
    [JsonPropertyName("comment")]
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Configuration digest for detecting concurrent modifications.
    /// </summary>
    [JsonPropertyName("digest")]
    [JsonProperty("digest")]
    public string? Digest { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"IP Set: {Name} | Comment: {Comment ?? "N/A"}";
    }
}
