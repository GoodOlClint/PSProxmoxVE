using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents a Proxmox VE asynchronous task as returned by the task-related endpoints
/// (e.g., /nodes/{node}/tasks, /nodes/{node}/tasks/{upid}/status).
/// Most mutating API calls return a UPID string that identifies the created task.
/// </summary>
public class PveTask
{
    /// <summary>
    /// The Unique Process Identifier for this task
    /// (e.g., "UPID:pve:000ABC:00000001:5F1234AB:qmstart:100:root@pam:").
    /// </summary>
    [JsonPropertyName("upid")]
    [JsonProperty("upid")]
    public string Upid { get; set; } = string.Empty;

    /// <summary>
    /// The task type string (e.g., "qmstart", "qmstop", "qmmigrate", "vzrestore").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The current task status: "running" while in progress, "stopped" when complete.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public string? Status { get; set; }

    /// <summary>
    /// The exit status when the task has stopped (e.g., "OK" on success, or an error message).
    /// </summary>
    [JsonPropertyName("exitstatus")]
    [JsonProperty("exitstatus")]
    public string? ExitStatus { get; set; }

    /// <summary>
    /// The node on which this task is executing.
    /// </summary>
    [JsonPropertyName("node")]
    [JsonProperty("node")]
    public string? Node { get; set; }

    /// <summary>
    /// Unix timestamp of when the task started.
    /// </summary>
    [JsonPropertyName("starttime")]
    [JsonProperty("starttime")]
    public long? StartTime { get; set; }

    /// <summary>
    /// Unix timestamp of when the task ended. Null if still running.
    /// </summary>
    [JsonPropertyName("endtime")]
    [JsonProperty("endtime")]
    public long? EndTime { get; set; }

    /// <summary>
    /// The user that initiated the task (e.g., "root@pam").
    /// </summary>
    [JsonPropertyName("user")]
    [JsonProperty("user")]
    public string? User { get; set; }

    /// <summary>
    /// The object ID the task is operating on (e.g., a VM ID or storage name).
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Returns true when the task completed successfully (stopped with exit status "OK").
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsSuccessful => Status == "stopped" && ExitStatus == "OK";

    /// <inheritdoc />
    public override string ToString()
    {
        var startStr = StartTime.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(StartTime.Value).ToString("yyyy-MM-dd HH:mm:ss")
            : "N/A";
        return $"Task [{Type ?? "unknown"}] UPID: {Upid} | Node: {Node ?? "N/A"} | "
             + $"Status: {Status ?? "unknown"} | ExitStatus: {ExitStatus ?? "-"} | "
             + $"User: {User ?? "N/A"} | Started: {startStr}";
    }
}
