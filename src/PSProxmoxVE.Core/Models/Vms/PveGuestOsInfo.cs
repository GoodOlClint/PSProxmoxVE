using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents OS information reported by the QEMU guest agent.
/// </summary>
public class PveGuestOsInfo
{
    /// <summary>The OS identifier (e.g., "mswindows", "linux").</summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>The OS name (e.g., "Microsoft Windows 10 Enterprise").</summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>The kernel release string.</summary>
    [JsonPropertyName("kernel-release")]
    [JsonProperty("kernel-release")]
    public string? KernelRelease { get; set; }

    /// <summary>The kernel version string.</summary>
    [JsonPropertyName("kernel-version")]
    [JsonProperty("kernel-version")]
    public string? KernelVersion { get; set; }

    /// <summary>The machine hardware name.</summary>
    [JsonPropertyName("machine")]
    [JsonProperty("machine")]
    public string? Machine { get; set; }

    /// <summary>The OS version details.</summary>
    [JsonPropertyName("version")]
    [JsonProperty("version")]
    public PveGuestOsVersion? Version { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var ver = Version != null ? $" {Version.Major}.{Version.Minor}" : string.Empty;
        return $"{Name ?? Id ?? "Unknown"}{ver} ({KernelRelease ?? "N/A"})";
    }
}

/// <summary>
/// Represents a guest OS version with major and minor components.
/// </summary>
public class PveGuestOsVersion
{
    /// <summary>The version identifier string.</summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>The major version number.</summary>
    [JsonPropertyName("major")]
    [JsonProperty("major")]
    public int? Major { get; set; }

    /// <summary>The minor version number.</summary>
    [JsonPropertyName("minor")]
    [JsonProperty("minor")]
    public int? Minor { get; set; }
}
