using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Firewall;

/// <summary>
/// Represents a Proxmox VE firewall IP alias as returned by
/// the firewall/aliases endpoints.
/// </summary>
public class PveFirewallAlias
{
    /// <summary>
    /// The alias name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The CIDR network or IP address (e.g. "10.0.0.0/8" or "192.168.1.1").
    /// </summary>
    [JsonProperty("cidr")]
    public string Cidr { get; set; } = string.Empty;

    /// <summary>
    /// Optional comment describing the alias.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Alias: {Name} → {Cidr}";
    }
}
