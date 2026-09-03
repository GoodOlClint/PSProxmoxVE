using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Models.Nodes;

/// <summary>
/// Represents the configuration of a Proxmox VE node,
/// as returned by the /nodes/{node}/config endpoint.
/// </summary>
public class PveNodeConfig
{
    /// <summary>
    /// Node specific ACME settings.
    /// </summary>
    [JsonProperty("acme")]
    public string? Acme { get; set; }

    /// <summary>
    /// RAM usage target for ballooning, in percent of total memory.
    /// </summary>
    [JsonProperty("ballooning-target")]
    public int? BallooningTarget { get; set; }

    /// <summary>
    /// Description for the node, shown in the web interface node notes panel.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// SHA1 digest of the current configuration file.
    /// </summary>
    [JsonProperty("digest")]
    public string? Digest { get; set; }

    /// <summary>
    /// The location of the node, overriding the datacenter config default.
    /// </summary>
    [JsonProperty("location")]
    public string? Location { get; set; }

    /// <summary>
    /// Initial delay in seconds before starting all on-boot Virtual Guests.
    /// </summary>
    [JsonProperty("startall-onboot-delay")]
    public int? StartAllOnbootDelay { get; set; }

    /// <summary>
    /// Node specific wake-on-LAN settings.
    /// </summary>
    [JsonProperty("wakeonlan")]
    public string? WakeOnLan { get; set; }

    /// <summary>
    /// Raw landing spot for any config key not mapped to a typed property above
    /// (e.g. per-domain "acmedomain0", "acmedomain1", ... entries).
    /// </summary>
    [JsonExtensionData]
    private IDictionary<string, JToken>? ExtensionData { get; set; }

    private Dictionary<string, object?>? _additionalProperties;

    /// <summary>
    /// Any node config keys not surfaced as a typed property above. Keys map to
    /// native .NET values so the dictionary works naturally in PowerShell pipelines.
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object?> AdditionalProperties =>
        _additionalProperties ??= ExtensionData == null
            ? new Dictionary<string, object?>()
            : ExtensionData.ToDictionary(kvp => kvp.Key, kvp => JsonHelper.ToNative(kvp.Value));

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Node Config | Description: {Description ?? "N/A"} | WakeOnLan: {WakeOnLan ?? "N/A"}";
    }
}
