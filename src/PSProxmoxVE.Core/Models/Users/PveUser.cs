using System;
using System.Linq;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Users;

/// <summary>
/// Represents a Proxmox VE user account as returned by the /access/users endpoint.
/// </summary>
public class PveUser
{
    /// <summary>
    /// The full user ID in the format "username@realm" (e.g., "admin@pam", "user@pve").
    /// </summary>
    [JsonProperty("userid")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The user's first name.
    /// </summary>
    [JsonProperty("firstname")]
    public string? FirstName { get; set; }

    /// <summary>
    /// The user's last name.
    /// </summary>
    [JsonProperty("lastname")]
    public string? LastName { get; set; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    [JsonProperty("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Indicates whether the user account is enabled (1) or disabled (0).
    /// </summary>
    [JsonProperty("enable")]
    public int? Enabled { get; set; }

    /// <summary>
    /// The authentication realm for this user (e.g., "pam", "pve", "ldap").
    /// </summary>
    [JsonProperty("realm")]
    public string? Realm { get; set; }

    /// <summary>
    /// Comma-separated list of groups this user is a member of.
    /// </summary>
    [JsonProperty("groups")]
    public string? Groups { get; set; }

    /// <summary>
    /// Optional comment or description for this user account.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Account expiry as a Unix timestamp. 0 or null means the account never expires.
    /// </summary>
    [JsonProperty("expire")]
    public long? Expire { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var fullName = string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var enabledStr = Enabled is 1 ? "enabled" : "disabled";
        var expireStr = Expire is > 0 ? DateTimeOffset.FromUnixTimeSeconds(Expire.Value).ToString("yyyy-MM-dd") : "never";
        return $"User: {UserId} | Name: {(string.IsNullOrEmpty(fullName) ? "N/A" : fullName)} | "
             + $"{enabledStr} | Groups: {Groups ?? "none"} | Expires: {expireStr}";
    }
}
