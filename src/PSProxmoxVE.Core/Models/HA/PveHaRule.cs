using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSProxmoxVE.Core.Models.HA;

/// <summary>
/// Represents a Proxmox VE HA rule from the /cluster/ha/rules endpoint.
/// Available in PVE 9.0 and later.
/// </summary>
public class PveHaRule
{
    /// <summary>
    /// The rule identifier.
    /// </summary>
    [JsonProperty("rule")]
    public string Rule { get; set; } = string.Empty;

    /// <summary>
    /// The rule type.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// An optional comment describing this HA rule.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// The rule state: "enabled" or "disabled".
    /// </summary>
    [JsonProperty("state")]
    public string? State { get; set; }

    /// <summary>
    /// The configuration digest, used for conflict detection on updates.
    /// </summary>
    [JsonProperty("digest")]
    public string? Digest { get; set; }

    /// <summary>
    /// Rule-specific properties that vary by rule type.
    /// </summary>
    [JsonProperty("properties")]
    public JObject? Properties { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"HA Rule: {Rule} | Type: {Type ?? "N/A"} | State: {State ?? "N/A"}";
    }
}
