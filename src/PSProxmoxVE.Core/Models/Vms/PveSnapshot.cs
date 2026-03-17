using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents a VM snapshot as returned by the /nodes/{node}/qemu/{vmid}/snapshot endpoint.
/// </summary>
public class PveSnapshot
{
    /// <summary>
    /// The snapshot name / identifier.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-readable description of what this snapshot captures.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Unix timestamp when the snapshot was created.
    /// </summary>
    [JsonPropertyName("snaptime")]
    [JsonProperty("snaptime")]
    public long? SnapTime { get; set; }

    /// <summary>
    /// Indicates whether the VM RAM state was saved with this snapshot (1) or not (0).
    /// </summary>
    [JsonPropertyName("vmstate")]
    [JsonProperty("vmstate")]
    public int? VmState { get; set; }

    /// <summary>
    /// The name of the parent snapshot, or null if this is the root snapshot.
    /// </summary>
    [JsonPropertyName("parent")]
    [JsonProperty("parent")]
    public string? Parent { get; set; }

    /// <summary>The VM ID this snapshot belongs to. Populated by cmdlets after retrieval.</summary>
    public int VmId { get; set; }

    /// <summary>The node the VM resides on. Populated by cmdlets after retrieval.</summary>
    public string? Node { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var timeStr = SnapTime.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(SnapTime.Value).ToString("yyyy-MM-dd HH:mm:ss")
            : "N/A";
        var stateStr = VmState is 1 ? " [+vmstate]" : string.Empty;
        var parentStr = Parent is not null ? $" | Parent: {Parent}" : string.Empty;
        return $"Snapshot: {Name}{stateStr} | Created: {timeStr} | "
             + $"Description: {Description ?? "(none)"}{parentStr}";
    }
}
