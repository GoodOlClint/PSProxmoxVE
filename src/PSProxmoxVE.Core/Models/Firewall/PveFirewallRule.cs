using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Firewall;

/// <summary>
/// Represents a Proxmox VE firewall rule as returned by
/// the firewall/rules endpoints at cluster, node, VM, or container level.
/// </summary>
public class PveFirewallRule
{
    /// <summary>
    /// Rule position (used for ordering and as identifier for updates/deletes).
    /// </summary>
    [JsonPropertyName("pos")]
    [JsonProperty("pos")]
    public int? Pos { get; set; }

    /// <summary>
    /// Rule type: "in", "out", or "group".
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Action to take: ACCEPT, DROP, or REJECT.
    /// </summary>
    [JsonPropertyName("action")]
    [JsonProperty("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Whether the rule is enabled (1) or disabled (0).
    /// </summary>
    [JsonPropertyName("enable")]
    [JsonProperty("enable")]
    public int? Enable { get; set; }

    /// <summary>
    /// Source address or alias.
    /// </summary>
    [JsonPropertyName("source")]
    [JsonProperty("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Destination address or alias.
    /// </summary>
    [JsonPropertyName("dest")]
    [JsonProperty("dest")]
    public string? Dest { get; set; }

    /// <summary>
    /// Protocol (e.g. tcp, udp, icmp).
    /// </summary>
    [JsonPropertyName("proto")]
    [JsonProperty("proto")]
    public string? Proto { get; set; }

    /// <summary>
    /// Destination port or port range.
    /// </summary>
    [JsonPropertyName("dport")]
    [JsonProperty("dport")]
    public string? Dport { get; set; }

    /// <summary>
    /// Source port or port range.
    /// </summary>
    [JsonPropertyName("sport")]
    [JsonProperty("sport")]
    public string? Sport { get; set; }

    /// <summary>
    /// Optional comment describing the rule.
    /// </summary>
    [JsonPropertyName("comment")]
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Predefined macro name (e.g. "SSH", "HTTP", "DNS").
    /// </summary>
    [JsonPropertyName("macro")]
    [JsonProperty("macro")]
    public string? Macro { get; set; }

    /// <summary>
    /// Log level for matched packets (e.g. "nolog", "info", "warning").
    /// </summary>
    [JsonPropertyName("log")]
    [JsonProperty("log")]
    public string? Log { get; set; }

    /// <summary>
    /// Network interface to match (e.g. "net0").
    /// </summary>
    [JsonPropertyName("iface")]
    [JsonProperty("iface")]
    public string? Iface { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var action = Macro != null ? $"{Action} ({Macro})" : Action;
        return $"Rule {Pos}: {Type} {action} | "
             + $"Src: {Source ?? "any"} → Dst: {Dest ?? "any"} | "
             + $"Proto: {Proto ?? "any"} DPort: {Dport ?? "any"}";
    }
}
