using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Models.Backup;

/// <summary>
/// Represents one guest not covered by any backup job, as returned by
/// the /cluster/backup-info/not-backed-up endpoint.
/// </summary>
public class PveBackupInfo
{
    /// <summary>
    /// Name of the guest.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Type of the guest ("qemu" or "lxc").
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// VMID of the guest.
    /// </summary>
    [JsonProperty("vmid")]
    public int? VmId { get; set; }

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
        return $"{Type ?? "guest"} {VmId?.ToString() ?? "?"} ({Name ?? "N/A"}) not backed up";
    }
}
