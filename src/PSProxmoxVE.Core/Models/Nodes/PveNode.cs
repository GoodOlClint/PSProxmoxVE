using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Nodes;

/// <summary>
/// Represents a Proxmox VE cluster node as returned by the /nodes endpoint.
/// </summary>
public class PveNode
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

    /// <inheritdoc />
    public override string ToString()
    {
        var memUsedMb = MemoryUsed.HasValue ? $"{MemoryUsed.Value / 1024 / 1024} MB" : "N/A";
        var memTotalMb = MemoryTotal.HasValue ? $"{MemoryTotal.Value / 1024 / 1024} MB" : "N/A";
        var uptimeStr = Uptime.HasValue ? TimeSpan.FromSeconds(Uptime.Value).ToString(@"d\.hh\:mm\:ss") : "N/A";
        return $"Node: {Name} | Status: {Status} | CPUs: {CpuCount?.ToString() ?? "N/A"} | "
             + $"Mem: {memUsedMb}/{memTotalMb} | Uptime: {uptimeStr} | PVE: {PveVersion ?? "N/A"}";
    }
}
