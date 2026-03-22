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
    [JsonProperty("pos")]
    public int? Pos { get; set; }

    /// <summary>
    /// Rule type: "in", "out", or "group".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Action to take: ACCEPT, DROP, or REJECT.
    /// </summary>
    [JsonProperty("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Whether the rule is enabled (1) or disabled (0).
    /// </summary>
    [JsonProperty("enable")]
    public int? Enable { get; set; }

    /// <summary>
    /// Source address or alias.
    /// </summary>
    [JsonProperty("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Destination address or alias.
    /// </summary>
    [JsonProperty("dest")]
    public string? Dest { get; set; }

    /// <summary>
    /// Protocol (e.g. tcp, udp, icmp).
    /// </summary>
    [JsonProperty("proto")]
    public string? Proto { get; set; }

    /// <summary>
    /// Destination port or port range.
    /// </summary>
    [JsonProperty("dport")]
    public string? Dport { get; set; }

    /// <summary>
    /// Source port or port range.
    /// </summary>
    [JsonProperty("sport")]
    public string? Sport { get; set; }

    /// <summary>
    /// Optional comment describing the rule.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Predefined macro name (e.g. "SSH", "HTTP", "DNS").
    /// </summary>
    [JsonProperty("macro")]
    public string? Macro { get; set; }

    /// <summary>
    /// Log level for matched packets (e.g. "nolog", "info", "warning").
    /// </summary>
    [JsonProperty("log")]
    public string? Log { get; set; }

    /// <summary>
    /// Network interface to match (e.g. "net0").
    /// </summary>
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
