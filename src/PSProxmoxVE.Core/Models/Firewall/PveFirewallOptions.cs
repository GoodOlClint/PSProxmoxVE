using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Firewall;

/// <summary>
/// Represents Proxmox VE firewall options as returned by
/// the firewall/options endpoints at cluster, node, VM, or container level.
/// </summary>
public class PveFirewallOptions
{
    /// <summary>
    /// Whether the firewall is enabled (1) or disabled (0).
    /// </summary>
    [JsonProperty("enable")]
    public int? Enable { get; set; }

    /// <summary>
    /// Default policy for incoming traffic (ACCEPT, DROP, REJECT).
    /// </summary>
    [JsonProperty("policy_in")]
    public string? PolicyIn { get; set; }

    /// <summary>
    /// Default policy for outgoing traffic (ACCEPT, DROP, REJECT).
    /// </summary>
    [JsonProperty("policy_out")]
    public string? PolicyOut { get; set; }

    /// <summary>
    /// Log level for incoming traffic (nolog, emerg, alert, crit, err, warning, notice, info, debug).
    /// </summary>
    [JsonProperty("log_level_in")]
    public string? LogLevelIn { get; set; }

    /// <summary>
    /// Log level for outgoing traffic.
    /// </summary>
    [JsonProperty("log_level_out")]
    public string? LogLevelOut { get; set; }

    /// <summary>
    /// Whether to allow DHCP traffic (VM/container level).
    /// </summary>
    [JsonProperty("dhcp")]
    public int? Dhcp { get; set; }

    /// <summary>
    /// Whether to allow NDP (IPv6 Neighbor Discovery Protocol).
    /// </summary>
    [JsonProperty("ndp")]
    public int? Ndp { get; set; }

    /// <summary>
    /// Whether to allow Router Advertisement.
    /// </summary>
    [JsonProperty("radv")]
    public int? Radv { get; set; }

    /// <summary>
    /// Whether to enable MAC address filter (VM/container level).
    /// </summary>
    [JsonProperty("macfilter")]
    public int? MacFilter { get; set; }

    /// <summary>
    /// Whether to enable IP filter (VM/container level).
    /// </summary>
    [JsonProperty("ipfilter")]
    public int? IpFilter { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var state = Enable == 1 ? "Enabled" : "Disabled";
        return $"Firewall: {state} | In: {PolicyIn ?? "N/A"} | Out: {PolicyOut ?? "N/A"}";
    }
}
