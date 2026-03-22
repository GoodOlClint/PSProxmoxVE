using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Network;

/// <summary>
/// Represents a Software-Defined Networking (SDN) IPAM plugin as returned by
/// the /cluster/sdn/ipams endpoint. Requires Proxmox VE 8.0+.
/// </summary>
public class PveSdnIpam
{
    /// <summary>
    /// The IPAM plugin identifier.
    /// </summary>
    [JsonProperty("ipam")]
    public string Ipam { get; set; } = string.Empty;

    /// <summary>
    /// The IPAM type (e.g. "pve", "netbox", "phpipam").
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the external IPAM service (for netbox/phpipam types).
    /// </summary>
    [JsonProperty("url")]
    public string? Url { get; set; }

    /// <summary>
    /// The API token for the external IPAM service.
    /// </summary>
    [JsonProperty("token")]
    public string? Token { get; set; }

    /// <summary>
    /// The configuration section identifier.
    /// </summary>
    [JsonProperty("section")]
    public int? Section { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"SDN IPAM: {Ipam} | Type: {Type}";
    }
}
