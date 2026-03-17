using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Storage;

/// <summary>
/// Represents a single content item (disk image, backup, ISO, template, etc.)
/// stored within a Proxmox VE storage pool, as returned by
/// the /nodes/{node}/storage/{storage}/content endpoint.
/// </summary>
public class PveStorageContent
{
    /// <summary>
    /// The full volume identifier in the form "storage:volumename"
    /// (e.g., "local-lvm:vm-100-disk-0").
    /// </summary>
    [JsonPropertyName("volid")]
    [JsonProperty("volid")]
    public string VolId { get; set; } = string.Empty;

    /// <summary>
    /// The content type of this volume (e.g., "images", "backup", "iso", "vztmpl", "rootdir").
    /// </summary>
    [JsonPropertyName("content")]
    [JsonProperty("content")]
    public string? Content { get; set; }

    /// <summary>
    /// The disk image format (e.g., "raw", "qcow2", "vmdk", "subvol").
    /// </summary>
    [JsonPropertyName("format")]
    [JsonProperty("format")]
    public string? Format { get; set; }

    /// <summary>
    /// The size of the volume in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonProperty("size")]
    public long? Size { get; set; }

    /// <summary>
    /// Unix timestamp of when the volume was created.
    /// </summary>
    [JsonPropertyName("ctime")]
    [JsonProperty("ctime")]
    public long? CreationTime { get; set; }

    /// <summary>
    /// The VM or container ID that owns this volume, if applicable (e.g., for disk images and backups).
    /// </summary>
    [JsonPropertyName("vmid")]
    [JsonProperty("vmid")]
    public int? VmId { get; set; }

    /// <summary>
    /// Optional notes or description attached to the volume (commonly used on backup files).
    /// </summary>
    [JsonPropertyName("notes")]
    [JsonProperty("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Verification status of the volume, if applicable (e.g., for backups verified with vzdump).
    /// </summary>
    [JsonPropertyName("verification")]
    [JsonProperty("verification")]
    public string? Verification { get; set; }

    /// <summary>The storage identifier this content resides on. Populated by cmdlets after retrieval.</summary>
    public string? Storage { get; set; }

    /// <summary>The node this content was retrieved from. Populated by cmdlets after retrieval.</summary>
    public string? Node { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var sizeGb = Size.HasValue ? $"{Size.Value / 1024.0 / 1024 / 1024:F2} GB" : "N/A";
        var created = CreationTime.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(CreationTime.Value).ToString("yyyy-MM-dd HH:mm:ss")
            : "N/A";
        return $"Volume: {VolId} | Content: {Content ?? "N/A"} | Format: {Format ?? "N/A"} | "
             + $"Size: {sizeGb} | Created: {created}";
    }
}
