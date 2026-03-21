using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Users;

/// <summary>
/// Represents a Proxmox VE authentication domain/realm as returned by the /access/domains endpoint.
/// </summary>
public class PveDomain
{
    /// <summary>The realm identifier (e.g., "pam", "pve", "myldap").</summary>
    [JsonPropertyName("realm")]
    [JsonProperty("realm")]
    public string Realm { get; set; } = string.Empty;

    /// <summary>The domain type (e.g., "pam", "pve", "ad", "ldap", "openid").</summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Optional comment/description for the domain.</summary>
    [JsonPropertyName("comment")]
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>Whether this is the default realm (1) or not (0).</summary>
    [JsonPropertyName("default")]
    [JsonProperty("default")]
    public int? Default { get; set; }

    /// <summary>TFA configuration string, if any.</summary>
    [JsonPropertyName("tfa")]
    [JsonProperty("tfa")]
    public string? Tfa { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var defaultStr = Default is 1 ? " [default]" : string.Empty;
        return $"Realm: {Realm}{defaultStr} | Type: {Type} | Comment: {Comment ?? "N/A"}";
    }
}
