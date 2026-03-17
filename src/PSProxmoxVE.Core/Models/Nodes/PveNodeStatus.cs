using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Nodes;

/// <summary>
/// Represents detailed status information for a single Proxmox VE node,
/// as returned by the /nodes/{node}/status endpoint.
/// </summary>
public class PveNodeStatus
{
    /// <summary>
    /// The name of the node.
    /// </summary>
    [JsonPropertyName("node")]
    [JsonProperty("node")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the node (e.g., "online" or "offline").
    /// </summary>
    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The total number of CPU cores available on the node.
    /// </summary>
    [JsonPropertyName("maxcpu")]
    [JsonProperty("maxcpu")]
    public int? CpuCount { get; set; }

    /// <summary>
    /// Total memory in bytes available on the node.
    /// </summary>
    [JsonPropertyName("maxmem")]
    [JsonProperty("maxmem")]
    public long? MemoryTotal { get; set; }

    /// <summary>
    /// Memory currently in use on the node, in bytes.
    /// </summary>
    [JsonPropertyName("mem")]
    [JsonProperty("mem")]
    public long? MemoryUsed { get; set; }

    /// <summary>
    /// Node uptime in seconds.
    /// </summary>
    [JsonPropertyName("uptime")]
    [JsonProperty("uptime")]
    public long? Uptime { get; set; }

    /// <summary>
    /// The version of Proxmox VE running on the node.
    /// </summary>
    [JsonPropertyName("pveversion")]
    [JsonProperty("pveversion")]
    public string? PveVersion { get; set; }

    /// <summary>
    /// The Linux kernel version running on the node.
    /// </summary>
    [JsonPropertyName("kversion")]
    [JsonProperty("kversion")]
    public string? KernelVersion { get; set; }

    /// <summary>
    /// The 1-minute, 5-minute, and 15-minute load averages for the node.
    /// </summary>
    [JsonPropertyName("loadavg")]
    [JsonProperty("loadavg")]
    public double[]? LoadAverage { get; set; }

    /// <summary>
    /// Total root filesystem size in bytes.
    /// </summary>
    [JsonPropertyName("rootfs.total")]
    [JsonProperty("rootfs.total")]
    public long? DiskTotal { get; set; }

    /// <summary>
    /// Used root filesystem space in bytes.
    /// </summary>
    [JsonPropertyName("rootfs.used")]
    [JsonProperty("rootfs.used")]
    public long? DiskUsed { get; set; }

    /// <summary>
    /// Current CPU utilization as a fraction between 0.0 and 1.0.
    /// </summary>
    [JsonPropertyName("cpu")]
    [JsonProperty("cpu")]
    public double? CpuUsage { get; set; }

    /// <summary>
    /// Memory utilization as a percentage (0–100), computed from MemoryUsed / MemoryTotal.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public double? MemoryUsage =>
        MemoryTotal.HasValue && MemoryTotal.Value > 0 && MemoryUsed.HasValue
            ? (double)MemoryUsed.Value / MemoryTotal.Value * 100.0
            : null;

    /// <summary>
    /// Total swap space in bytes.
    /// </summary>
    [JsonPropertyName("swap.total")]
    [JsonProperty("swap.total")]
    public long? SwapTotal { get; set; }

    /// <summary>
    /// Used swap space in bytes.
    /// </summary>
    [JsonPropertyName("swap.used")]
    [JsonProperty("swap.used")]
    public long? SwapUsed { get; set; }

    /// <summary>
    /// Total bytes received over the network since boot.
    /// </summary>
    [JsonPropertyName("netin")]
    [JsonProperty("netin")]
    public long? NetworkIn { get; set; }

    /// <summary>
    /// Total bytes transmitted over the network since boot.
    /// </summary>
    [JsonPropertyName("netout")]
    [JsonProperty("netout")]
    public long? NetworkOut { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var memUsedMb = MemoryUsed.HasValue ? $"{MemoryUsed.Value / 1024 / 1024} MB" : "N/A";
        var memTotalMb = MemoryTotal.HasValue ? $"{MemoryTotal.Value / 1024 / 1024} MB" : "N/A";
        var cpuPct = CpuUsage.HasValue ? $"{CpuUsage.Value * 100:F1}%" : "N/A";
        var diskUsedGb = DiskUsed.HasValue ? $"{DiskUsed.Value / 1024 / 1024 / 1024} GB" : "N/A";
        var diskTotalGb = DiskTotal.HasValue ? $"{DiskTotal.Value / 1024 / 1024 / 1024} GB" : "N/A";
        var uptimeStr = Uptime.HasValue ? TimeSpan.FromSeconds(Uptime.Value).ToString(@"d\.hh\:mm\:ss") : "N/A";
        return $"Node: {Name} | Status: {Status} | CPU: {cpuPct} | "
             + $"Mem: {memUsedMb}/{memTotalMb} ({MemoryUsage:F1}%) | "
             + $"Disk: {diskUsedGb}/{diskTotalGb} | Uptime: {uptimeStr}";
    }
}
