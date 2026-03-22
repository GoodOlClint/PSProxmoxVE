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
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The reference name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional comment.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Type}: {Name}";
    }
}
