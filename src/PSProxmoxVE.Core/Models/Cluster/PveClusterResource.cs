using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Cluster;

/// <summary>
/// Represents a resource entry returned by the /cluster/resources endpoint.
/// Resources can be VMs, containers, nodes, storage, or SDN objects.
/// </summary>
public class PveClusterResource
{
    /// <summary>
    /// The unique resource identifier (e.g., "node/pve1", "qemu/100", "storage/local").
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The resource type: "vm" (QEMU), "lxc" (container), "node", "storage", or "sdn".
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The current status of the resource (e.g., "running", "stopped", "online").
    /// </summary>
    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public string? Status { get; set; }

    /// <summary>
    /// The display name of the resource.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The node on which this resource resides.
    /// </summary>
    [JsonPropertyName("node")]
    [JsonProperty("node")]
    public string? Node { get; set; }

    /// <summary>
    /// The pool this resource belongs to, if any.
    /// </summary>
    [JsonPropertyName("pool")]
    [JsonProperty("pool")]
    public string? Pool { get; set; }

    /// <summary>
    /// Uptime in seconds.
    /// </summary>
    [JsonPropertyName("uptime")]
    [JsonProperty("uptime")]
    public long? Uptime { get; set; }

    /// <summary>
    /// Maximum number of CPUs/cores available to this resource.
    /// </summary>
    [JsonPropertyName("maxcpu")]
    [JsonProperty("maxcpu")]
    public double? MaxCpu { get; set; }

    /// <summary>
    /// Current CPU usage as a fraction (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("cpu")]
    [JsonProperty("cpu")]
    public double? Cpu { get; set; }

    /// <summary>
    /// Maximum memory in bytes.
    /// </summary>
    [JsonPropertyName("maxmem")]
    [JsonProperty("maxmem")]
    public long? MaxMem { get; set; }

    /// <summary>
    /// Current memory usage in bytes.
    /// </summary>
    [JsonPropertyName("mem")]
    [JsonProperty("mem")]
    public long? Mem { get; set; }

    /// <summary>
    /// Maximum disk space in bytes.
    /// </summary>
    [JsonPropertyName("maxdisk")]
    [JsonProperty("maxdisk")]
    public long? MaxDisk { get; set; }

    /// <summary>
    /// Current disk usage in bytes.
    /// </summary>
    [JsonPropertyName("disk")]
    [JsonProperty("disk")]
    public long? Disk { get; set; }

    /// <summary>
    /// Whether this resource is a template (1) or not (0). Only for VM/LXC types.
    /// </summary>
    [JsonPropertyName("template")]
    [JsonProperty("template")]
    public int? Template { get; set; }

    /// <summary>
    /// The VM or container ID. Only for VM/LXC types.
    /// </summary>
    [JsonPropertyName("vmid")]
    [JsonProperty("vmid")]
    public int? VmId { get; set; }

    /// <summary>
    /// The HA (High Availability) state of the resource, if managed by HA.
    /// </summary>
    [JsonPropertyName("hastate")]
    [JsonProperty("hastate")]
    public string? HaState { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Type ?? "unknown"}: {Name ?? Id ?? "N/A"} | Node: {Node ?? "N/A"} | Status: {Status ?? "N/A"}";
    }
}
