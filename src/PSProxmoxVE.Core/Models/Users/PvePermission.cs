using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Users;

/// <summary>
/// Represents a single Access Control List (ACL) entry as returned by the /access/acl endpoint.
/// An ACL entry grants a specific role to a user or group on a particular resource path.
/// </summary>
public class PvePermission
{
    /// <summary>
    /// The resource path this ACL entry applies to (e.g., "/", "/nodes/pve", "/vms/100").
    /// </summary>
    [JsonPropertyName("path")]
    [JsonProperty("path")]
    public string? Path { get; set; }

    /// <summary>
    /// The role ID granted by this ACL entry (e.g., "Administrator", "PVEVMAdmin").
    /// </summary>
    [JsonPropertyName("roleid")]
    [JsonProperty("roleid")]
    public string? RoleId { get; set; }

    /// <summary>
    /// Indicates whether the permission propagates to sub-paths (1) or is restricted to the exact path (0).
    /// </summary>
    [JsonPropertyName("propagate")]
    [JsonProperty("propagate")]
    public int? Propagate { get; set; }

    /// <summary>
    /// The user ID or group ID (with "@" prefix for groups) that this ACL entry applies to.
    /// Maps to the "ugid" JSON field.
    /// </summary>
    [JsonPropertyName("ugid")]
    [JsonProperty("ugid")]
    public string? UserId { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var propagateStr = Propagate is 1 ? " [propagate]" : string.Empty;
        return $"ACL: {UserId ?? "N/A"} | Path: {Path ?? "/"} | Role: {RoleId ?? "N/A"}{propagateStr}";
    }
}
