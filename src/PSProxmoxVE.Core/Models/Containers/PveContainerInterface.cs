using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Containers;

/// <summary>
/// Represents a network interface on an LXC container.
/// </summary>
public class PveContainerInterface
{
    /// <summary>The interface name (e.g., "eth0", "lo").</summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>The hardware (MAC) address of the interface.</summary>
    [JsonProperty("hwaddr")]
    public string? HwAddr { get; set; }

    /// <summary>The IPv4 address assigned to this interface.</summary>
    [JsonProperty("inet")]
    public string? Inet { get; set; }

    /// <summary>The IPv6 address assigned to this interface.</summary>
    [JsonProperty("inet6")]
    public string? Inet6 { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var addr = Inet ?? Inet6 ?? "no address";
        return $"{Name ?? "Unknown"} ({HwAddr ?? "N/A"}): {addr}";
    }
}
