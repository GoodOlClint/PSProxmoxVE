using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Cluster;

/// <summary>
/// Represents a node entry from the GET /cluster/config/nodes endpoint.
/// </summary>
public class PveClusterConfigNode
{
    /// <summary>
    /// The node name.
    /// </summary>
    [JsonProperty("node")]
    public string? Name { get; set; }

    /// <summary>
    /// The numeric node ID within the cluster Corosync configuration.
    /// </summary>
    [JsonProperty("nodeid")]
    public int? NodeId { get; set; }

    /// <summary>
    /// The address used for Corosync ring 0 communication.
    /// </summary>
    [JsonProperty("ring0_addr")]
    public string? Ring0Addr { get; set; }

    /// <summary>
    /// The address used for Corosync ring 1 communication.
    /// </summary>
    [JsonProperty("ring1_addr")]
    public string? Ring1Addr { get; set; }

    /// <summary>
    /// The number of quorum votes assigned to this node.
    /// </summary>
    [JsonProperty("quorum_votes")]
    public int? QuorumVotes { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var nodeIdStr = NodeId.HasValue ? $" (ID {NodeId})" : string.Empty;
        return $"Node: {Name ?? "N/A"}{nodeIdStr} | Ring0: {Ring0Addr ?? "N/A"}";
    }
}
