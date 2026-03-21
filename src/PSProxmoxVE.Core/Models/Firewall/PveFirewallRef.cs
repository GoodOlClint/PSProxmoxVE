using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Firewall;

/// <summary>
/// Represents a Proxmox VE firewall reference item (alias, ipset, etc.)
/// as returned by the firewall/refs endpoints.
/// </summary>
public class PveFirewallRef
{
    /// <summary>
    /// The reference type (e.g. "alias", "ipset").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The reference name.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional comment.
    /// </summary>
    [JsonPropertyName("comment")]
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Type}: {Name}";
    }
}
