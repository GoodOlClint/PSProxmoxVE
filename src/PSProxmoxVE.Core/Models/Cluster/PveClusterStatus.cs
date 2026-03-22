using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Cluster;

/// <summary>
/// Represents an entry in the mixed array returned by the /cluster/status endpoint.
/// Each entry has a Type of either "cluster" (cluster-wide info) or "node" (per-node info).
/// Filter by <see cref="Type"/> to distinguish cluster vs. node entries.
/// </summary>
public class PveClusterStatus
{
    /// <summary>
    /// The entry type: "cluster" for the cluster-wide record, or "node" for a node record.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The name of the cluster (present when Type == "cluster") or the node name (when Type == "node").
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The total number of nodes in the cluster. Only present when Type == "cluster".
    /// </summary>
    [JsonProperty("nodes")]
    public int? Nodes { get; set; }

    /// <summary>
    /// Indicates whether the cluster currently has quorum (1) or not (0).
    /// Without quorum, most write operations are blocked to maintain data consistency.
    /// Only present when Type == "cluster".
    /// </summary>
    [JsonProperty("quorate")]
    public int? Quorate { get; set; }

    /// <summary>
    /// The cluster configuration version number. Only present when Type == "cluster".
    /// </summary>
    [JsonProperty("version")]
    public int? Version { get; set; }

    /// <summary>
    /// The IP address used for cluster communication on this node.
    /// Only present when Type == "node".
    /// </summary>
    [JsonProperty("ip")]
    public string? Ip { get; set; }

    /// <summary>
    /// Indicates whether this node is currently online (1) or offline (0).
    /// Only present when Type == "node".
    /// </summary>
    [JsonProperty("online")]
    public int? Online { get; set; }

    /// <summary>
    /// Indicates whether this is the local node making the request (1) or a remote node (0).
    /// Only present when Type == "node".
    /// </summary>
    [JsonProperty("local")]
    public int? Local { get; set; }

    /// <summary>
    /// The numeric node ID within the cluster Corosync configuration.
    /// Only present when Type == "node".
    /// </summary>
    [JsonProperty("nodeid")]
    public int? NodeId { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Type == "node")
        {
            var onlineStr = Online is 1 ? "online" : "offline";
            var localStr = Local is 1 ? " [local]" : string.Empty;
            return $"Node: {Name ?? "N/A"}{localStr} | IP: {Ip ?? "N/A"} | {onlineStr}";
        }
        var quorateStr = Quorate is 1 ? "quorate" : "NO QUORUM";
        return $"Cluster: {Name ?? "N/A"} | Nodes: {Nodes?.ToString() ?? "N/A"} | "
             + $"{quorateStr} | Config Version: {Version?.ToString() ?? "N/A"}";
    }
}
