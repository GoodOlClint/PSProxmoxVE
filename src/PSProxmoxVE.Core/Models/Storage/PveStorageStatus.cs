using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Storage;

/// <summary>
/// Represents the status of a Proxmox VE storage on a specific node,
/// as returned by the /nodes/{node}/storage/{storage}/status endpoint.
/// </summary>
public class PveStorageStatus
{
    /// <summary>Total capacity in bytes.</summary>
    [JsonPropertyName("total")]
    [JsonProperty("total")]
    public long? Total { get; set; }

    /// <summary>Used capacity in bytes.</summary>
    [JsonPropertyName("used")]
    [JsonProperty("used")]
    public long? Used { get; set; }

    /// <summary>Available (free) capacity in bytes.</summary>
    [JsonPropertyName("avail")]
    [JsonProperty("avail")]
    public long? Available { get; set; }

    /// <summary>Whether the storage is currently active (1) or not (0).</summary>
    [JsonPropertyName("active")]
    [JsonProperty("active")]
    public int? Active { get; set; }

    /// <summary>Comma-separated list of supported content types.</summary>
    [JsonPropertyName("content")]
    [JsonProperty("content")]
    public string? Content { get; set; }

    /// <summary>Whether the storage is enabled (1) or disabled (0).</summary>
    [JsonPropertyName("enabled")]
    [JsonProperty("enabled")]
    public int? Enabled { get; set; }

    /// <summary>Whether the storage is shared across cluster nodes (1) or node-local (0).</summary>
    [JsonPropertyName("shared")]
    [JsonProperty("shared")]
    public int? Shared { get; set; }

    /// <summary>The storage backend type.</summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>The storage identifier. Populated by the cmdlet after retrieval.</summary>
    public string? Storage { get; set; }

    /// <summary>The node name. Populated by the cmdlet after retrieval.</summary>
    public string? Node { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var usedGb = Used.HasValue ? $"{Used.Value / 1024.0 / 1024 / 1024:F1} GB" : "N/A";
        var totalGb = Total.HasValue ? $"{Total.Value / 1024.0 / 1024 / 1024:F1} GB" : "N/A";
        var availGb = Available.HasValue ? $"{Available.Value / 1024.0 / 1024 / 1024:F1} GB" : "N/A";
        var activeStr = Active is 1 ? "active" : "inactive";
        return $"Storage: {Storage ?? "N/A"} | Type: {Type ?? "N/A"} | {activeStr} | "
             + $"Used: {usedGb}/{totalGb} | Free: {availGb}";
    }
}
