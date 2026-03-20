using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents a QEMU/KVM virtual machine as returned by the cluster or node VM list endpoints.
/// </summary>
public class PveVm
{
    /// <summary>
    /// The unique VM identifier.
    /// </summary>
    [JsonPropertyName("vmid")]
    [JsonProperty("vmid")]
    public int VmId { get; set; }

    /// <summary>
    /// The name of the virtual machine.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The runtime status from the VM list endpoint (e.g., "running", "stopped").
    /// Note: reports "running" for both running AND paused VMs. For accurate pause
    /// detection, use <see cref="EffectiveStatus"/> or <see cref="QmpStatus"/>,
    /// which are populated when using Get-PveVm -Detailed.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public string? Status { get; set; }

    /// <summary>
    /// The node on which the VM resides.
    /// </summary>
    [JsonPropertyName("node")]
    [JsonProperty("node")]
    public string? Node { get; set; }

    /// <summary>
    /// The number of virtual CPU cores assigned to the VM.
    /// </summary>
    [JsonPropertyName("cpus")]
    [JsonProperty("cpus")]
    public int? CpuCount { get; set; }

    /// <summary>
    /// Maximum memory allocated to the VM, in bytes.
    /// </summary>
    [JsonPropertyName("maxmem")]
    [JsonProperty("maxmem")]
    public long? MaxMem { get; set; }

    /// <summary>
    /// Maximum disk size allocated to the VM, in bytes.
    /// </summary>
    [JsonPropertyName("maxdisk")]
    [JsonProperty("maxdisk")]
    public long? MaxDisk { get; set; }

    /// <summary>
    /// VM uptime in seconds.
    /// </summary>
    [JsonPropertyName("uptime")]
    [JsonProperty("uptime")]
    public long? Uptime { get; set; }

    /// <summary>
    /// Semicolon-separated list of tags assigned to the VM.
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonProperty("tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// Indicates whether the VM is a template (1) or a regular VM (0).
    /// </summary>
    [JsonPropertyName("template")]
    [JsonProperty("template")]
    public int? Template { get; set; }

    /// <summary>
    /// The current lock type applied to the VM, if any (e.g., "migrate", "backup").
    /// </summary>
    [JsonPropertyName("lock")]
    [JsonProperty("lock")]
    public string? Lock { get; set; }

    /// <summary>
    /// The process ID of the running QEMU process, if applicable.
    /// </summary>
    [JsonPropertyName("pid")]
    [JsonProperty("pid")]
    public int? Pid { get; set; }

    /// <summary>
    /// The QMP (QEMU Machine Protocol) status string (e.g., "running", "paused", "stopped").
    /// Only populated from the status/current endpoint (Get-PveVm -Detailed), not the list endpoint.
    /// </summary>
    [JsonPropertyName("qmpstatus")]
    [JsonProperty("qmpstatus")]
    public string? QmpStatus { get; set; }

    /// <summary>
    /// The effective runtime status, preferring QmpStatus over Status for accurate
    /// pause detection. Returns QmpStatus when available (from -Detailed), otherwise Status.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? EffectiveStatus => QmpStatus ?? Status;

    /// <summary>
    /// Indicates whether the QEMU guest agent is running (non-zero) inside the VM.
    /// </summary>
    [JsonPropertyName("agent")]
    [JsonProperty("agent")]
    public int? AgentStatus { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var maxMemMb = MaxMem.HasValue ? $"{MaxMem.Value / 1024 / 1024} MB" : "N/A";
        var maxDiskGb = MaxDisk.HasValue ? $"{MaxDisk.Value / 1024 / 1024 / 1024} GB" : "N/A";
        var uptimeStr = Uptime.HasValue ? TimeSpan.FromSeconds(Uptime.Value).ToString(@"d\.hh\:mm\:ss") : "N/A";
        var templateStr = Template is 1 ? " [template]" : string.Empty;
        return $"VM {VmId}{templateStr}: {Name ?? "(unnamed)"} | Node: {Node ?? "N/A"} | "
             + $"Status: {EffectiveStatus ?? "N/A"} | CPUs: {CpuCount?.ToString() ?? "N/A"} | "
             + $"Mem: {maxMemMb} | Disk: {maxDiskGb} | Uptime: {uptimeStr}";
    }
}
