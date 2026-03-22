using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Firewall;

/// <summary>
/// Represents an entry within a Proxmox VE firewall IP set as returned by
/// the firewall/ipset/{name} endpoints.
/// </summary>
public class PveFirewallIpSetEntry
{
    /// <summary>
    /// The CIDR network or IP address (e.g. "192.168.1.0/24" or "10.0.0.1").
    /// </summary>
    [JsonProperty("cidr")]
    public string Cidr { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is an exclusion entry (1 = exclude from set).
    /// </summary>
    [JsonProperty("nomatch")]
    public int? NoMatch { get; set; }

    /// <summary>
    /// Optional comment describing the entry.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var prefix = NoMatch == 1 ? "!" : "";
        return $"{prefix}{Cidr}";
    }
}
