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
    [JsonProperty("controller")]
    public string Controller { get; set; } = string.Empty;

    /// <summary>
    /// The controller type (e.g. "evpn", "bgp").
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The Autonomous System Number for BGP/EVPN.
    /// </summary>
    [JsonProperty("asn")]
    public int? Asn { get; set; }

    /// <summary>
    /// Comma-separated list of BGP peer addresses.
    /// </summary>
    [JsonProperty("peers")]
    public string? Peers { get; set; }

    /// <summary>
    /// The node this controller is configured on.
    /// </summary>
    [JsonProperty("node")]
    public string? Node { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"SDN Controller: {Controller} | Type: {Type} | ASN: {Asn?.ToString() ?? "N/A"}";
    }
}
