using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.HA;

/// <summary>
/// Represents a Proxmox VE HA group from the /cluster/ha/groups endpoint.
/// </summary>
public class PveHaGroup
{
    /// <summary>
    /// The HA group name.
    /// </summary>
    [JsonProperty("group")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of nodes with optional priorities, e.g. "node1:2,node2:1".
    /// </summary>
    [JsonProperty("nodes")]
    public string? Nodes { get; set; }

    /// <summary>
    /// Whether the group is restricted (1) or not (0). When restricted, resources can
    /// only run on group members.
    /// </summary>
    [JsonProperty("restricted")]
    public int? Restricted { get; set; }

    /// <summary>
    /// Whether failback is disabled (1) or enabled (0). When nofailback is set, the
    /// resource will not automatically migrate back to its preferred node after recovery.
    /// </summary>
    [JsonProperty("nofailback")]
    public int? NoFailback { get; set; }

    /// <summary>
    /// An optional comment describing this HA group.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// The object type.
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The configuration digest, used for conflict detection on updates.
    /// </summary>
    [JsonProperty("digest")]
    public string? Digest { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"HA Group: {Group} | Nodes: {Nodes ?? "N/A"}";
    }
}
