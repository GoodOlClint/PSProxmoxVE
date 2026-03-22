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
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The job type (typically "vzdump").
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether the job is enabled (1) or disabled (0).
    /// </summary>
    [JsonProperty("enabled")]
    public int? Enabled { get; set; }

    /// <summary>
    /// The cron-style schedule (e.g. "0 2 * * *").
    /// </summary>
    [JsonProperty("schedule")]
    public string? Schedule { get; set; }

    /// <summary>
    /// The target storage for backup files.
    /// </summary>
    [JsonProperty("storage")]
    public string? Storage { get; set; }

    /// <summary>
    /// Backup mode: "snapshot", "suspend", or "stop".
    /// </summary>
    [JsonProperty("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// Comma-separated list of VM/container IDs to back up, or empty for all.
    /// </summary>
    [JsonProperty("vmid")]
    public string? VmId { get; set; }

    /// <summary>
    /// Whether to back up all VMs/containers (1 = all).
    /// </summary>
    [JsonProperty("all")]
    public int? All { get; set; }

    /// <summary>
    /// Compression algorithm (e.g. "zstd", "lzo", "gzip").
    /// </summary>
    [JsonProperty("compress")]
    public string? Compress { get; set; }

    /// <summary>
    /// Deprecated — maximum number of backup files to keep. Use prune-backups instead.
    /// </summary>
    [JsonProperty("maxfiles")]
    public int? MaxFiles { get; set; }

    /// <summary>
    /// Retention policy string (e.g. "keep-daily=7,keep-weekly=4").
    /// </summary>
    [JsonProperty("prune-backups")]
    public string? PruneBackups { get; set; }

    /// <summary>
    /// Template for backup notes.
    /// </summary>
    [JsonProperty("notes-template")]
    public string? NotesTemplate { get; set; }

    /// <summary>
    /// Optional comment describing the backup job.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Mail notification setting (e.g. "always", "failure").
    /// </summary>
    [JsonProperty("mailnotification")]
    public string? MailNotification { get; set; }

    /// <summary>
    /// Email address to send notifications to.
    /// </summary>
    [JsonProperty("mailto")]
    public string? MailTo { get; set; }

    /// <summary>
    /// Specific node to run the backup on (empty = any node).
    /// </summary>
    [JsonProperty("node")]
    public string? Node { get; set; }

    /// <summary>
    /// Comma-separated list of excluded VM/container IDs.
    /// </summary>
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
