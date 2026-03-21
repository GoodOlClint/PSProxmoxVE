using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Backup;

/// <summary>
/// Represents a Proxmox VE scheduled backup job as returned by
/// the /cluster/backup endpoint.
/// </summary>
public class PveBackupJob
{
    /// <summary>
    /// The backup job identifier.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The job type (typically "vzdump").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether the job is enabled (1) or disabled (0).
    /// </summary>
    [JsonPropertyName("enabled")]
    [JsonProperty("enabled")]
    public int? Enabled { get; set; }

    /// <summary>
    /// The cron-style schedule (e.g. "0 2 * * *").
    /// </summary>
    [JsonPropertyName("schedule")]
    [JsonProperty("schedule")]
    public string? Schedule { get; set; }

    /// <summary>
    /// The target storage for backup files.
    /// </summary>
    [JsonPropertyName("storage")]
    [JsonProperty("storage")]
    public string? Storage { get; set; }

    /// <summary>
    /// Backup mode: "snapshot", "suspend", or "stop".
    /// </summary>
    [JsonPropertyName("mode")]
    [JsonProperty("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// Comma-separated list of VM/container IDs to back up, or empty for all.
    /// </summary>
    [JsonPropertyName("vmid")]
    [JsonProperty("vmid")]
    public string? VmId { get; set; }

    /// <summary>
    /// Whether to back up all VMs/containers (1 = all).
    /// </summary>
    [JsonPropertyName("all")]
    [JsonProperty("all")]
    public int? All { get; set; }

    /// <summary>
    /// Compression algorithm (e.g. "zstd", "lzo", "gzip").
    /// </summary>
    [JsonPropertyName("compress")]
    [JsonProperty("compress")]
    public string? Compress { get; set; }

    /// <summary>
    /// Deprecated — maximum number of backup files to keep. Use prune-backups instead.
    /// </summary>
    [JsonPropertyName("maxfiles")]
    [JsonProperty("maxfiles")]
    public int? MaxFiles { get; set; }

    /// <summary>
    /// Retention policy string (e.g. "keep-daily=7,keep-weekly=4").
    /// </summary>
    [JsonPropertyName("prune-backups")]
    [JsonProperty("prune-backups")]
    public string? PruneBackups { get; set; }

    /// <summary>
    /// Template for backup notes.
    /// </summary>
    [JsonPropertyName("notes-template")]
    [JsonProperty("notes-template")]
    public string? NotesTemplate { get; set; }

    /// <summary>
    /// Optional comment describing the backup job.
    /// </summary>
    [JsonPropertyName("comment")]
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Mail notification setting (e.g. "always", "failure").
    /// </summary>
    [JsonPropertyName("mailnotification")]
    [JsonProperty("mailnotification")]
    public string? MailNotification { get; set; }

    /// <summary>
    /// Email address to send notifications to.
    /// </summary>
    [JsonPropertyName("mailto")]
    [JsonProperty("mailto")]
    public string? MailTo { get; set; }

    /// <summary>
    /// Specific node to run the backup on (empty = any node).
    /// </summary>
    [JsonPropertyName("node")]
    [JsonProperty("node")]
    public string? Node { get; set; }

    /// <summary>
    /// Comma-separated list of excluded VM/container IDs.
    /// </summary>
    [JsonPropertyName("exclude")]
    [JsonProperty("exclude")]
    public string? Exclude { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var state = Enabled == 1 ? "Enabled" : "Disabled";
        return $"Backup Job: {Id} | {state} | Schedule: {Schedule ?? "N/A"} | "
             + $"Storage: {Storage ?? "N/A"} | Mode: {Mode ?? "N/A"}";
    }
}
