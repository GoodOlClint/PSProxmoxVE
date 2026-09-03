using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Models.Nodes;

/// <summary>
/// Represents the DNS configuration of a Proxmox VE node,
/// as returned by the /nodes/{node}/dns endpoint.
/// </summary>
public class PveNodeDns
{
    /// <summary>
    /// First name server IP address.
    /// </summary>
    [JsonProperty("dns1")]
    public string? Dns1 { get; set; }

    /// <summary>
    /// Second name server IP address.
    /// </summary>
    [JsonProperty("dns2")]
    public string? Dns2 { get; set; }

    /// <summary>
    /// Third name server IP address.
    /// </summary>
    [JsonProperty("dns3")]
    public string? Dns3 { get; set; }

    /// <summary>
    /// Search domain for host-name lookup.
    /// </summary>
    [JsonProperty("search")]
    public string? Search { get; set; }

    /// <summary>
    /// Raw landing spot for any DNS config key not mapped to a typed property above.
    /// </summary>
    [JsonExtensionData]
    private IDictionary<string, JToken>? ExtensionData { get; set; }

    private Dictionary<string, object?>? _additionalProperties;

    /// <summary>
    /// Any DNS config keys not surfaced as a typed property above. Keys map to
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
        return $"DNS | Search: {Search ?? "N/A"} | {Dns1 ?? "N/A"}, {Dns2 ?? "N/A"}, {Dns3 ?? "N/A"}";
    }
}
