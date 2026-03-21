using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSProxmoxVE.Core.Models.Cluster;

/// <summary>
/// Represents a Proxmox VE resource pool as returned by the /pools endpoint.
/// </summary>
public class PvePool
{
    /// <summary>
    /// The pool identifier.
    /// </summary>
    [JsonPropertyName("poolid")]
    [JsonProperty("poolid")]
    public string? PoolId { get; set; }

    /// <summary>
    /// An optional comment or description for the pool.
    /// </summary>
    [JsonPropertyName("comment")]
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// The pool members (VMs, containers, storage). Returned as a raw array
    /// when querying a specific pool.
    /// </summary>
    [JsonPropertyName("members")]
    [JsonProperty("members")]
    public JArray? Members { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var memberCount = Members?.Count ?? 0;
        return $"Pool: {PoolId ?? "N/A"} | Comment: {Comment ?? "-"} | Members: {memberCount}";
    }
}
