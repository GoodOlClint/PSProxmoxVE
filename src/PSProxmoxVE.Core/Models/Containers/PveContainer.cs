using System;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Containers;

/// <summary>
/// Represents a Linux Container (LXC) as returned by the cluster or node container list endpoints.
/// </summary>
public class PveContainer
{
    /// <summary>
    /// The unique container identifier.
    /// </summary>
    [JsonProperty("vmid")]
    public int VmId { get; set; }

    /// <summary>
    /// The hostname / display name of the container.
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The current runtime status of the container (e.g., "running", "stopped").
    /// </summary>
    [JsonProperty("status")]
    public string? Status { get; set; }

    /// <summary>
    /// The node on which the container resides.
    /// </summary>
    [JsonProperty("node")]
    public string? Node { get; set; }

    /// <summary>
    /// The number of CPU cores assigned to the container.
    /// </summary>
    [JsonProperty("cpus")]
    public int? CpuCount { get; set; }

    /// <summary>
    /// Maximum memory allocated to the container, in bytes.
    /// </summary>
    [JsonProperty("maxmem")]
    public long? MaxMem { get; set; }

    /// <summary>
    /// Maximum swap space allocated to the container, in bytes.
    /// </summary>
    [JsonProperty("maxswap")]
    public long? MaxSwap { get; set; }

    /// <summary>
    /// Root filesystem disk size allocated to the container, in bytes.
    /// </summary>
    [JsonProperty("maxdisk")]
    public long? RootFsSize { get; set; }

    /// <summary>
    /// The OS template type used for this container (e.g., "debian", "ubuntu").
    /// </summary>
    [JsonProperty("ostype")]
    public string? OsType { get; set; }

    /// <summary>
    /// Indicates whether the container runs in unprivileged mode (1) or privileged mode (0).
    /// </summary>
    [JsonProperty("unprivileged")]
    public int? Unprivileged { get; set; }

    /// <summary>
    /// The current lock type applied to the container, if any (e.g., "migrate", "backup").
    /// </summary>
    [JsonProperty("lock")]
    public string? Lock { get; set; }

    /// <summary>
    /// Container uptime in seconds.
    /// </summary>
    [JsonProperty("uptime")]
    public long? Uptime { get; set; }

    /// <summary>
    /// Semicolon-separated list of tags assigned to the container.
    /// </summary>
    [JsonProperty("tags")]
    public string? Tags { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var maxMemMb = MaxMem.HasValue ? $"{MaxMem.Value / 1024 / 1024} MB" : "N/A";
        var rootFsGb = RootFsSize.HasValue ? $"{RootFsSize.Value / 1024 / 1024 / 1024} GB" : "N/A";
        var uptimeStr = Uptime.HasValue ? TimeSpan.FromSeconds(Uptime.Value).ToString(@"d\.hh\:mm\:ss") : "N/A";
        var privStr = Unprivileged is 1 ? "unprivileged" : "privileged";
        return $"CT {VmId}: {Name ?? "(unnamed)"} | Node: {Node ?? "N/A"} | "
             + $"Status: {Status ?? "N/A"} | CPUs: {CpuCount?.ToString() ?? "N/A"} | "
             + $"Mem: {maxMemMb} | Disk: {rootFsGb} | {privStr} | Uptime: {uptimeStr}";
    }
}
