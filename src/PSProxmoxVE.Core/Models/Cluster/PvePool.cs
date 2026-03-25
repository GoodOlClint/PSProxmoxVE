using System.Collections.Generic;
using Newtonsoft.Json;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Models.Cluster;

/// <summary>
/// Represents a Proxmox VE resource pool as returned by the /pools endpoint.
/// </summary>
public class PvePool
{
    /// <summary>
    /// The pool identifier.
    /// </summary>
    [JsonProperty("poolid")]
    public string? PoolId { get; set; }

    /// <summary>
    /// An optional comment or description for the pool.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// The pool members (VMs, containers, storage). Returned as a list of dictionaries
    /// when querying a specific pool.
    /// </summary>
    [JsonProperty("members")]
    [JsonConverter(typeof(NativeListConverter))]
    public List<Dictionary<string, object?>>? Members { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var memberCount = Members?.Count ?? 0;
        return $"Pool: {PoolId ?? "N/A"} | Comment: {Comment ?? "-"} | Members: {memberCount}";
    }
}
