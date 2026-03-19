using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents a network interface reported by the QEMU guest agent.
/// </summary>
public class PveGuestNetworkInterface
{
    /// <summary>The interface name (e.g., "eth0", "ens18", "lo").</summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The hardware (MAC) address of the interface.</summary>
    [JsonPropertyName("hardware-address")]
    [JsonProperty("hardware-address")]
    public string? HardwareAddress { get; set; }

    /// <summary>The IP addresses assigned to this interface.</summary>
    [JsonPropertyName("ip-addresses")]
    [JsonProperty("ip-addresses")]
    public PveGuestIpAddress[] IpAddresses { get; set; } = System.Array.Empty<PveGuestIpAddress>();

    public override string ToString()
    {
        var ips = IpAddresses.Length > 0
            ? string.Join(", ", System.Array.ConvertAll(IpAddresses, ip => ip.ToString()))
            : "no addresses";
        return $"{Name} ({HardwareAddress ?? "N/A"}): {ips}";
    }
}
