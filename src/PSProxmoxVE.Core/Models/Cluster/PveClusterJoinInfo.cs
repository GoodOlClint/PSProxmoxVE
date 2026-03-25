using System.Collections.Generic;
using Newtonsoft.Json;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Models.Cluster;

/// <summary>
/// Represents the response from GET /cluster/config/join, containing
/// the information needed to join this cluster.
/// </summary>
public class PveClusterJoinInfo
{
    /// <summary>
    /// The SHA digest of the current cluster configuration.
    /// </summary>
    [JsonProperty("config_digest")]
    public string? ConfigDigest { get; set; }

    /// <summary>
    /// The list of nodes in the cluster with their connection details.
    /// </summary>
    [JsonProperty("nodelist")]
    [JsonConverter(typeof(NativeListConverter))]
    public List<Dictionary<string, object?>>? Nodelist { get; set; }

    /// <summary>
    /// The preferred node to connect to when joining.
    /// </summary>
    [JsonProperty("preferred_node")]
    public string? PreferredNode { get; set; }

    /// <summary>
    /// The Corosync totem configuration.
    /// </summary>
    [JsonProperty("totem")]
    [JsonConverter(typeof(NativeDictionaryConverter))]
    public Dictionary<string, object?>? Totem { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var nodeCount = Nodelist?.Count ?? 0;
        return $"JoinInfo: PreferredNode={PreferredNode ?? "N/A"} | Nodes={nodeCount}";
    }
}
