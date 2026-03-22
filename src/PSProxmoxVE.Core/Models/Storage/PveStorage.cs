using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Storage;

/// <summary>
/// Represents a Proxmox VE storage entry as returned by the /storage or /nodes/{node}/storage endpoints.
/// </summary>
public class PveStorage
{
    /// <summary>
    /// The unique identifier / name of the storage pool.
    /// </summary>
    [JsonProperty("storage")]
    public string Storage { get; set; } = string.Empty;

    /// <summary>
    /// The storage backend type (e.g., "dir", "lvm", "zfspool", "nfs", "cifs", "rbd").
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Comma-separated list of content types this storage supports
    /// (e.g., "images,rootdir,backup,snippets,iso,vztmpl").
    /// </summary>
    [JsonProperty("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Total capacity of the storage pool, in bytes.
    /// </summary>
    [JsonProperty("total")]
    public long? Total { get; set; }

    /// <summary>
    /// Used capacity of the storage pool, in bytes.
    /// </summary>
    [JsonProperty("used")]
    public long? Used { get; set; }

    /// <summary>
    /// Available (free) capacity of the storage pool, in bytes.
    /// </summary>
    [JsonProperty("avail")]
    public long? Available { get; set; }

    /// <summary>
    /// Indicates whether the storage is enabled (1) or disabled (0) in the configuration.
    /// </summary>
    [JsonProperty("enabled")]
    public int? Enabled { get; set; }

    /// <summary>
    /// Indicates whether the storage is shared across all cluster nodes (1) or node-local (0).
    /// </summary>
    [JsonProperty("shared")]
    public int? Shared { get; set; }

    /// <summary>
    /// Indicates whether the storage is currently active and reachable (1) or not (0).
    /// </summary>
    [JsonProperty("active")]
    public int? Active { get; set; }

    /// <summary>The node this storage entry is associated with. Populated by cmdlets after retrieval.</summary>
    public string? Node { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var usedGb = Used.HasValue ? $"{Used.Value / 1024.0 / 1024 / 1024:F1} GB" : "N/A";
        var totalGb = Total.HasValue ? $"{Total.Value / 1024.0 / 1024 / 1024:F1} GB" : "N/A";
        var availGb = Available.HasValue ? $"{Available.Value / 1024.0 / 1024 / 1024:F1} GB" : "N/A";
        var activeStr = Active is 1 ? "active" : "inactive";
        var sharedStr = Shared is 1 ? "shared" : "local";
        return $"Storage: {Storage} | Type: {Type ?? "N/A"} | {activeStr} | {sharedStr} | "
             + $"Used: {usedGb}/{totalGb} | Free: {availGb} | Content: {Content ?? "N/A"}";
    }
}
