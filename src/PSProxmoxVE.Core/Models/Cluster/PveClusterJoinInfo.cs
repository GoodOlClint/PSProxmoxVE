using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    /// This is a complex nested structure returned as a JArray.
    /// </summary>
    [JsonProperty("nodelist")]
    public JArray? Nodelist { get; set; }

    /// <summary>
    /// The preferred node to connect to when joining.
    /// </summary>
    [JsonProperty("preferred_node")]
    public string? PreferredNode { get; set; }

    /// <summary>
    /// The Corosync totem configuration as a raw JSON object.
    /// </summary>
    [JsonProperty("totem")]
    public JObject? Totem { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var nodeCount = Nodelist?.Count ?? 0;
        return $"JoinInfo: PreferredNode={PreferredNode ?? "N/A"} | Nodes={nodeCount}";
    }
}
