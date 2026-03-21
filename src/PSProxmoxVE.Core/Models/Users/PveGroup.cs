using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Users;

/// <summary>
/// Represents a Proxmox VE group as returned by the /access/groups endpoint.
/// </summary>
public class PveGroup
{
    /// <summary>The unique group identifier.</summary>
    [JsonPropertyName("groupid")]
    [JsonProperty("groupid")]
    public string GroupId { get; set; } = string.Empty;

    /// <summary>Optional comment/description for the group.</summary>
    [JsonPropertyName("comment")]
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>Comma-separated list of user IDs that belong to this group.</summary>
    [JsonPropertyName("users")]
    [JsonProperty("users")]
    public string? Users { get; set; }

    /// <summary>Array of member user IDs. Populated when available.</summary>
    [JsonPropertyName("members")]
    [JsonProperty("members")]
    public string[]? Members { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Group: {GroupId} | Comment: {Comment ?? "N/A"} | Users: {Users ?? "none"}";
    }
}
