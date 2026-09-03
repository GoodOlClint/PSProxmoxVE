using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Models.Cluster;

/// <summary>
/// Represents one entry of the directory index returned by GET /cluster/config.
/// The endpoint's item schema documents no named properties ("properties": {}),
/// but its "links" metadata gives the child-URL template as "{name}", so every
/// entry carries a "name" key (e.g. "nodes", "totem", "qdevice", "join",
/// "apiversion"). Any other key lands in <see cref="AdditionalProperties"/>.
/// </summary>
public class PveClusterConfigEntry
{
    /// <summary>
    /// The sub-resource name (e.g. "nodes", "totem", "qdevice", "join", "apiversion").
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Raw landing spot for any key not mapped to a typed property above.
    /// </summary>
    [JsonExtensionData]
    private IDictionary<string, JToken>? ExtensionData { get; set; }

    private Dictionary<string, object?>? _additionalProperties;

    /// <summary>
    /// Any keys not surfaced as a typed property above. Keys map to native
    /// .NET values so the dictionary works naturally in PowerShell pipelines.
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object?> AdditionalProperties =>
        _additionalProperties ??= ExtensionData == null
            ? new Dictionary<string, object?>()
            : ExtensionData.ToDictionary(kvp => kvp.Key, kvp => JsonHelper.ToNative(kvp.Value));

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Cluster Config Entry: {Name ?? "N/A"}";
    }
}
