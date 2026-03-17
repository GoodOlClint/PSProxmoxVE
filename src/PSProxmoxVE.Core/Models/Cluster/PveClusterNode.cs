using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Cluster;

/// <summary>
/// Represents a node entry within the cluster status response,
/// as returned by the /cluster/status endpoint when the item type is "node".
/// </summary>
public class PveClusterNode
{
    /// <summary>
    /// The node name (e.g., "pve", "pve2").
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this node is currently online (1) or offline (0).
    /// </summary>
    [JsonPropertyName("online")]
    [JsonProperty("online")]
    public int? Online { get; set; }

    /// <summary>
    /// Indicates whether this is the local node making the request (1) or a remote node (0).
    /// </summary>
    [JsonPropertyName("local")]
    [JsonProperty("local")]
    public int? Local { get; set; }

    /// <summary>
    /// The numeric node ID within the cluster Corosync configuration.
    /// </summary>
    [JsonPropertyName("nodeid")]
    [JsonProperty("nodeid")]
    public int? NodeId { get; set; }

    /// <summary>
    /// The IP address used for cluster communication on this node.
    /// </summary>
    [JsonPropertyName("ip")]
    [JsonProperty("ip")]
    public string? Ip { get; set; }

    /// <summary>
    /// The cluster membership level of this node (e.g., "c" for configured member).
    /// </summary>
    [JsonPropertyName("level")]
    [JsonProperty("level")]
    public string? Level { get; set; }

    /// <summary>
    /// The item type as returned by the cluster status endpoint (always "node" for this model).
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var onlineStr = Online is 1 ? "online" : "offline";
        var localStr = Local is 1 ? " [local]" : string.Empty;
        return $"Node: {Name}{localStr} | ID: {NodeId?.ToString() ?? "N/A"} | "
             + $"IP: {Ip ?? "N/A"} | {onlineStr}";
    }
}
