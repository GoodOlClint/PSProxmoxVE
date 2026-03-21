using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Firewall;

/// <summary>
/// Represents a Proxmox VE firewall security group as returned by
/// the /cluster/firewall/groups endpoint.
/// </summary>
public class PveFirewallGroup
{
    /// <summary>
    /// The security group name.
    /// </summary>
    [JsonPropertyName("group")]
    [JsonProperty("group")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Optional comment describing the group.
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
        return $"Firewall Group: {Group} | Comment: {Comment ?? "N/A"}";
    }
}
