using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents filesystem information reported by the QEMU guest agent.
/// </summary>
public class PveGuestFsInfo
{
    /// <summary>The filesystem name/device.</summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>The mount point path.</summary>
    [JsonPropertyName("mountpoint")]
    [JsonProperty("mountpoint")]
    public string? MountPoint { get; set; }

    /// <summary>The filesystem type (e.g., "ext4", "ntfs").</summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>Total size of the filesystem in bytes.</summary>
    [JsonPropertyName("total-bytes")]
    [JsonProperty("total-bytes")]
    public long? TotalBytes { get; set; }

    /// <summary>Used space on the filesystem in bytes.</summary>
    [JsonPropertyName("used-bytes")]
    [JsonProperty("used-bytes")]
    public long? UsedBytes { get; set; }

    /// <summary>List of disk devices backing this filesystem.</summary>
    [JsonPropertyName("disk")]
    [JsonProperty("disk")]
    public string[]? DiskList { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var used = UsedBytes.HasValue && TotalBytes.HasValue && TotalBytes.Value > 0
            ? $" ({UsedBytes.Value * 100 / TotalBytes.Value}% used)"
            : string.Empty;
        return $"{MountPoint ?? Name ?? "Unknown"} [{Type ?? "?"}]{used}";
    }
}
