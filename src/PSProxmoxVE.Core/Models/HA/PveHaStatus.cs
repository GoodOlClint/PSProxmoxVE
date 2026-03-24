using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.HA;

/// <summary>
/// Represents an entry from the /cluster/ha/status/current endpoint.
/// Entries may be of type "quorum", "manager", or "service".
/// </summary>
public class PveHaStatus
{
    /// <summary>
    /// The status entry identifier.
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The entry type: "quorum", "manager", or "service".
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The cluster node name associated with this entry.
    /// </summary>
    [JsonProperty("node")]
    public string? Node { get; set; }

    /// <summary>
    /// The current status text.
    /// </summary>
    [JsonProperty("status")]
    public string? Status { get; set; }

    /// <summary>
    /// The Unix timestamp of the status entry.
    /// </summary>
    [JsonProperty("timestamp")]
    public long? Timestamp { get; set; }

    /// <summary>
    /// The CRM (Cluster Resource Manager) state.
    /// </summary>
    [JsonProperty("crm_state")]
    public string? CrmState { get; set; }

    /// <summary>
    /// The requested state for a service entry.
    /// </summary>
    [JsonProperty("request_state")]
    public string? RequestState { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"HA Status: {Id ?? "N/A"} | Node: {Node ?? "N/A"} | Status: {Status ?? "N/A"}";
    }
}
