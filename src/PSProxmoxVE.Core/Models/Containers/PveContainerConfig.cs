using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Containers;

/// <summary>
/// Represents the configuration of a Linux Container (LXC),
/// as returned by the /nodes/{node}/lxc/{vmid}/config endpoint.
/// </summary>
public class PveContainerConfig
{
    // -------------------------------------------------------------------------
    // Identity / OS
    // -------------------------------------------------------------------------

    /// <summary>
    /// The container hostname.
    /// </summary>
    [JsonPropertyName("hostname")]
    [JsonProperty("hostname")]
    public string? Hostname { get; set; }

    /// <summary>
    /// The OS type used for the container (e.g., "debian", "ubuntu", "alpine").
    /// </summary>
    [JsonPropertyName("ostype")]
    [JsonProperty("ostype")]
    public string? OsType { get; set; }

    /// <summary>
    /// Container architecture (e.g., "amd64", "arm64").
    /// </summary>
    [JsonPropertyName("arch")]
    [JsonProperty("arch")]
    public string? Arch { get; set; }

    /// <summary>
    /// Indicates whether the container runs unprivileged (1) or privileged (0).
    /// </summary>
    [JsonPropertyName("unprivileged")]
    [JsonProperty("unprivileged")]
    public int? Unprivileged { get; set; }

    // -------------------------------------------------------------------------
    // Resources
    // -------------------------------------------------------------------------

    /// <summary>
    /// Memory limit in megabytes.
    /// </summary>
    [JsonPropertyName("memory")]
    [JsonProperty("memory")]
    public int? Memory { get; set; }

    /// <summary>
    /// Swap space limit in megabytes. 0 disables swap.
    /// </summary>
    [JsonPropertyName("swap")]
    [JsonProperty("swap")]
    public int? Swap { get; set; }

    /// <summary>
    /// Number of CPU cores allocated to the container.
    /// </summary>
    [JsonPropertyName("cores")]
    [JsonProperty("cores")]
    public int? Cores { get; set; }

    /// <summary>
    /// Root filesystem configuration string (e.g., "local-lvm:8").
    /// </summary>
    [JsonPropertyName("rootfs")]
    [JsonProperty("rootfs")]
    public string? RootFs { get; set; }

    // -------------------------------------------------------------------------
    // Metadata
    // -------------------------------------------------------------------------

    /// <summary>
    /// Human-readable description or notes for the container.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Semicolon-separated list of tags assigned to the container.
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonProperty("tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// When set to 1, prevents the container from being deleted or modified accidentally.
    /// </summary>
    [JsonPropertyName("protection")]
    [JsonProperty("protection")]
    public int? Protection { get; set; }

    /// <summary>
    /// Startup / shutdown order and delay configuration string.
    /// </summary>
    [JsonPropertyName("startup")]
    [JsonProperty("startup")]
    public string? Startup { get; set; }

    /// <summary>
    /// Feature flags for the container (e.g., "nesting=1,keyctl=1").
    /// </summary>
    [JsonPropertyName("features")]
    [JsonProperty("features")]
    public string? Features { get; set; }

    // -------------------------------------------------------------------------
    // Network interfaces (0-7)
    // -------------------------------------------------------------------------

    /// <summary>Network interface 0 configuration string.</summary>
    [JsonPropertyName("net0")]
    [JsonProperty("net0")]
    public string? Net0 { get; set; }

    /// <summary>Network interface 1 configuration string.</summary>
    [JsonPropertyName("net1")]
    [JsonProperty("net1")]
    public string? Net1 { get; set; }

    /// <summary>Network interface 2 configuration string.</summary>
    [JsonPropertyName("net2")]
    [JsonProperty("net2")]
    public string? Net2 { get; set; }

    /// <summary>Network interface 3 configuration string.</summary>
    [JsonPropertyName("net3")]
    [JsonProperty("net3")]
    public string? Net3 { get; set; }

    /// <summary>Network interface 4 configuration string.</summary>
    [JsonPropertyName("net4")]
    [JsonProperty("net4")]
    public string? Net4 { get; set; }

    /// <summary>Network interface 5 configuration string.</summary>
    [JsonPropertyName("net5")]
    [JsonProperty("net5")]
    public string? Net5 { get; set; }

    /// <summary>Network interface 6 configuration string.</summary>
    [JsonPropertyName("net6")]
    [JsonProperty("net6")]
    public string? Net6 { get; set; }

    /// <summary>Network interface 7 configuration string.</summary>
    [JsonPropertyName("net7")]
    [JsonProperty("net7")]
    public string? Net7 { get; set; }

    // -------------------------------------------------------------------------
    // Mount points (0-7)
    // -------------------------------------------------------------------------

    /// <summary>Mount point 0 configuration string (e.g., "local-lvm:10,mp=/data").</summary>
    [JsonPropertyName("mp0")]
    [JsonProperty("mp0")]
    public string? Mp0 { get; set; }

    /// <summary>Mount point 1 configuration string.</summary>
    [JsonPropertyName("mp1")]
    [JsonProperty("mp1")]
    public string? Mp1 { get; set; }

    /// <summary>Mount point 2 configuration string.</summary>
    [JsonPropertyName("mp2")]
    [JsonProperty("mp2")]
    public string? Mp2 { get; set; }

    /// <summary>Mount point 3 configuration string.</summary>
    [JsonPropertyName("mp3")]
    [JsonProperty("mp3")]
    public string? Mp3 { get; set; }

    /// <summary>Mount point 4 configuration string.</summary>
    [JsonPropertyName("mp4")]
    [JsonProperty("mp4")]
    public string? Mp4 { get; set; }

    /// <summary>Mount point 5 configuration string.</summary>
    [JsonPropertyName("mp5")]
    [JsonProperty("mp5")]
    public string? Mp5 { get; set; }

    /// <summary>Mount point 6 configuration string.</summary>
    [JsonPropertyName("mp6")]
    [JsonProperty("mp6")]
    public string? Mp6 { get; set; }

    /// <summary>Mount point 7 configuration string.</summary>
    [JsonPropertyName("mp7")]
    [JsonProperty("mp7")]
    public string? Mp7 { get; set; }

    // -------------------------------------------------------------------------
    // DNS / SSH
    // -------------------------------------------------------------------------

    /// <summary>
    /// DNS nameserver(s) for the container.
    /// </summary>
    [JsonPropertyName("nameserver")]
    [JsonProperty("nameserver")]
    public string? Nameserver { get; set; }

    /// <summary>
    /// DNS search domain for the container.
    /// </summary>
    [JsonPropertyName("searchdomain")]
    [JsonProperty("searchdomain")]
    public string? Searchdomain { get; set; }

    /// <summary>
    /// SSH public keys injected into the container's root account.
    /// </summary>
    [JsonPropertyName("ssh-public-keys")]
    [JsonProperty("ssh-public-keys")]
    public string? SshPublicKeys { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Config | Hostname: {Hostname ?? "N/A"} | OS: {OsType ?? "N/A"} | "
             + $"Cores: {Cores?.ToString() ?? "N/A"} | Memory: {Memory?.ToString() ?? "N/A"} MB | "
             + $"Swap: {Swap?.ToString() ?? "N/A"} MB | Arch: {Arch ?? "N/A"} | "
             + $"Unprivileged: {(Unprivileged is 1 ? "yes" : "no")}";
    }
}
