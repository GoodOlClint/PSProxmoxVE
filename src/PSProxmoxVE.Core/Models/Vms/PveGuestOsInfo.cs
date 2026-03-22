using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents OS information reported by the QEMU guest agent.
/// </summary>
public class PveGuestOsInfo
{
    /// <summary>The OS identifier (e.g., "mswindows", "linux").</summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>The OS name (e.g., "Microsoft Windows 10 Enterprise").</summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>The kernel release string.</summary>
    [JsonProperty("kernel-release")]
    public string? KernelRelease { get; set; }

    /// <summary>The kernel version string.</summary>
    [JsonProperty("kernel-version")]
    public string? KernelVersion { get; set; }

    /// <summary>The machine hardware name.</summary>
    [JsonProperty("machine")]
    public string? Machine { get; set; }

    /// <summary>The OS version details.</summary>
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
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>The major version number.</summary>
    [JsonProperty("major")]
    public int? Major { get; set; }

    /// <summary>The minor version number.</summary>
    [JsonProperty("minor")]
    public int? Minor { get; set; }
}
