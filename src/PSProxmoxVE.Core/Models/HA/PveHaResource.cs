using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.HA;

/// <summary>
/// Represents a Proxmox VE HA resource from the /cluster/ha/resources endpoint.
/// </summary>
public class PveHaResource
{
    /// <summary>
    /// The HA resource ID, e.g. "vm:100" or "ct:200".
    /// </summary>
    [JsonProperty("sid")]
    public string Sid { get; set; } = string.Empty;

    /// <summary>
    /// The requested state: "started", "stopped", "disabled", or "ignored".
    /// </summary>
    [JsonProperty("state")]
    public string? State { get; set; }

    /// <summary>
    /// The HA group this resource is assigned to.
    /// </summary>
    [JsonProperty("group")]
    public string? Group { get; set; }

    /// <summary>
    /// Maximum number of relocations before the resource is placed in an error state.
    /// </summary>
    [JsonProperty("max_relocate")]
    public int? MaxRelocate { get; set; }

    /// <summary>
    /// Maximum number of restart attempts before the resource is placed in an error state.
    /// </summary>
    [JsonProperty("max_restart")]
    public int? MaxRestart { get; set; }

    /// <summary>
    /// An optional comment describing this HA resource.
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// The resource type: "vm" or "ct".
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
        return $"HA Resource: {Sid} | State: {State ?? "N/A"} | Group: {Group ?? "N/A"}";
    }
}
