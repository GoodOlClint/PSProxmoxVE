using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Network;

/// <summary>
/// Represents a Software-Defined Networking (SDN) controller as returned by
/// the /cluster/sdn/controllers endpoint. Requires Proxmox VE 8.0+.
/// </summary>
public class PveSdnController
{
    /// <summary>
    /// The controller identifier.
    /// </summary>
    [JsonPropertyName("controller")]
    [JsonProperty("controller")]
    public string Controller { get; set; } = string.Empty;

    /// <summary>
    /// The controller type (e.g. "evpn", "bgp").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The Autonomous System Number for BGP/EVPN.
    /// </summary>
    [JsonPropertyName("asn")]
    [JsonProperty("asn")]
    public int? Asn { get; set; }

    /// <summary>
    /// Comma-separated list of BGP peer addresses.
    /// </summary>
    [JsonPropertyName("peers")]
    [JsonProperty("peers")]
    public string? Peers { get; set; }

    /// <summary>
    /// The node this controller is configured on.
    /// </summary>
    [JsonPropertyName("node")]
    [JsonProperty("node")]
    public string? Node { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"SDN Controller: {Controller} | Type: {Type} | ASN: {Asn?.ToString() ?? "N/A"}";
    }
}
