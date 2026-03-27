using System;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Users;

/// <summary>
/// Represents a Proxmox VE API token as returned by the /access/users/{userid}/token endpoints.
/// </summary>
public class PveApiToken
{
    /// <summary>
    /// The user ID that owns this token (e.g., "admin@pam").
    /// Not returned by the API — populated by the cmdlet from the request path.
    /// </summary>
    [JsonProperty("userid")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The token identifier (the part after "!"), e.g., "automation".
    /// </summary>
    [JsonProperty("tokenid")]
    public string TokenId { get; set; } = string.Empty;

    /// <summary>
    /// The full token identifier in "user@realm!tokenid" format, e.g., "admin@pam!automation".
    /// Computed from UserId and TokenId. The "full-tokenid" JSON field from New-PveApiToken
    /// is captured by <see cref="RawFullTokenId"/> for deserialization, but this property
    /// always returns a computed value so it works for list/get endpoints too.
    /// </summary>
    public string FullTokenId =>
        string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(TokenId)
            ? RawFullTokenId ?? string.Empty
            : $"{UserId}!{TokenId}";

    /// <summary>
    /// Raw "full-tokenid" value from the API (only present on token creation responses).
    /// </summary>
    [JsonProperty("full-tokenid")]
    public string? RawFullTokenId { get; set; }

    /// <summary>
    /// The token secret UUID. <b>Only present on creation</b> — store it immediately,
    /// as it cannot be retrieved again.
    /// </summary>
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Optional comment or description for this token.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Token expiry as a Unix timestamp. 0 or null means the token never expires.
    /// </summary>
    [JsonProperty("expire")]
    public long? Expire { get; set; }

    /// <summary>
    /// Whether privilege separation is enabled (1) or disabled (0).
    /// When enabled, the token's permissions are the intersection of the user's ACLs and
    /// any explicit ACLs granted to the token itself.
    /// </summary>
    [JsonProperty("privsep")]
    public int? PrivilegeSeparation { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var full  = FullTokenId ?? $"{UserId}!{TokenId}";
        var privsep = PrivilegeSeparation is 1 ? "privsep" : "no-privsep";
        var expireStr = Expire is > 0
            ? DateTimeOffset.FromUnixTimeSeconds(Expire.Value).ToString("yyyy-MM-dd")
            : "never";
        return $"Token: {full} | {privsep} | Expires: {expireStr}";
    }
}
