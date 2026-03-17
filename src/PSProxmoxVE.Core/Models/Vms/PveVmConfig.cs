using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents the configuration of a QEMU/KVM virtual machine,
/// as returned by the /nodes/{node}/qemu/{vmid}/config endpoint.
/// </summary>
public class PveVmConfig
{
    // -------------------------------------------------------------------------
    // CPU / Memory
    // -------------------------------------------------------------------------

    /// <summary>
    /// Number of CPU cores per socket.
    /// </summary>
    [JsonPropertyName("cores")]
    [JsonProperty("cores")]
    public int? Cores { get; set; }

    /// <summary>
    /// Number of CPU sockets.
    /// </summary>
    [JsonPropertyName("sockets")]
    [JsonProperty("sockets")]
    public int? Sockets { get; set; }

    /// <summary>
    /// Memory size in megabytes.
    /// </summary>
    [JsonPropertyName("memory")]
    [JsonProperty("memory")]
    public int? Memory { get; set; }

    /// <summary>
    /// Emulated CPU type (e.g., "host", "x86-64-v2-AES").
    /// </summary>
    [JsonPropertyName("cpu")]
    [JsonProperty("cpu")]
    public string? CpuType { get; set; }

    // -------------------------------------------------------------------------
    // Firmware / Machine
    // -------------------------------------------------------------------------

    /// <summary>
    /// BIOS implementation to use: "seabios" (default) or "ovmf" (UEFI).
    /// </summary>
    [JsonPropertyName("bios")]
    [JsonProperty("bios")]
    public string? Bios { get; set; }

    /// <summary>
    /// Emulated machine type (e.g., "q35", "i440fx").
    /// </summary>
    [JsonPropertyName("machine")]
    [JsonProperty("machine")]
    public string? Machine { get; set; }

    // -------------------------------------------------------------------------
    // Boot / Args
    // -------------------------------------------------------------------------

    /// <summary>
    /// Boot order specification string.
    /// </summary>
    [JsonPropertyName("boot")]
    [JsonProperty("boot")]
    public string? Boot { get; set; }

    /// <summary>
    /// Arbitrary QEMU command-line arguments appended to the QEMU launch command.
    /// </summary>
    [JsonPropertyName("args")]
    [JsonProperty("args")]
    public string? Args { get; set; }

    // -------------------------------------------------------------------------
    // Metadata
    // -------------------------------------------------------------------------

    /// <summary>
    /// Human-readable description or notes for the VM.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Semicolon-separated list of tags assigned to the VM.
    /// </summary>
    [JsonPropertyName("tags")]
    [JsonProperty("tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// When set to 1, prevents the VM from being deleted or modified accidentally.
    /// </summary>
    [JsonPropertyName("protection")]
    [JsonProperty("protection")]
    public int? Protection { get; set; }

    /// <summary>
    /// NUMA topology enabled (1) or disabled (0).
    /// </summary>
    [JsonPropertyName("numa")]
    [JsonProperty("numa")]
    public int? Numa { get; set; }

    /// <summary>
    /// VirtIO balloon device target memory in MB. 0 disables ballooning.
    /// </summary>
    [JsonPropertyName("balloon")]
    [JsonProperty("balloon")]
    public int? Balloon { get; set; }

    /// <summary>
    /// Guest OS type hint (e.g., "l26" for Linux 2.6+, "win10").
    /// </summary>
    [JsonPropertyName("ostype")]
    [JsonProperty("ostype")]
    public string? OsType { get; set; }

    // -------------------------------------------------------------------------
    // VirtIO disk slots (0–3, most commonly used)
    // -------------------------------------------------------------------------

    /// <summary>VirtIO disk slot 0 configuration string.</summary>
    [JsonPropertyName("virtio0")]
    [JsonProperty("virtio0")]
    public string? Virtio0 { get; set; }

    /// <summary>VirtIO disk slot 1 configuration string.</summary>
    [JsonPropertyName("virtio1")]
    [JsonProperty("virtio1")]
    public string? Virtio1 { get; set; }

    /// <summary>VirtIO disk slot 2 configuration string.</summary>
    [JsonPropertyName("virtio2")]
    [JsonProperty("virtio2")]
    public string? Virtio2 { get; set; }

    /// <summary>VirtIO disk slot 3 configuration string.</summary>
    [JsonPropertyName("virtio3")]
    [JsonProperty("virtio3")]
    public string? Virtio3 { get; set; }

    // -------------------------------------------------------------------------
    // SCSI disk slots (0–7)
    // -------------------------------------------------------------------------

    /// <summary>SCSI disk slot 0 configuration string.</summary>
    [JsonPropertyName("scsi0")]
    [JsonProperty("scsi0")]
    public string? Scsi0 { get; set; }

    /// <summary>SCSI disk slot 1 configuration string.</summary>
    [JsonPropertyName("scsi1")]
    [JsonProperty("scsi1")]
    public string? Scsi1 { get; set; }

    /// <summary>SCSI disk slot 2 configuration string.</summary>
    [JsonPropertyName("scsi2")]
    [JsonProperty("scsi2")]
    public string? Scsi2 { get; set; }

    /// <summary>SCSI disk slot 3 configuration string.</summary>
    [JsonPropertyName("scsi3")]
    [JsonProperty("scsi3")]
    public string? Scsi3 { get; set; }

    /// <summary>SCSI disk slot 4 configuration string.</summary>
    [JsonPropertyName("scsi4")]
    [JsonProperty("scsi4")]
    public string? Scsi4 { get; set; }

    /// <summary>SCSI disk slot 5 configuration string.</summary>
    [JsonPropertyName("scsi5")]
    [JsonProperty("scsi5")]
    public string? Scsi5 { get; set; }

    /// <summary>SCSI disk slot 6 configuration string.</summary>
    [JsonPropertyName("scsi6")]
    [JsonProperty("scsi6")]
    public string? Scsi6 { get; set; }

    /// <summary>SCSI disk slot 7 configuration string.</summary>
    [JsonPropertyName("scsi7")]
    [JsonProperty("scsi7")]
    public string? Scsi7 { get; set; }

    // -------------------------------------------------------------------------
    // IDE disk slots (0–3)
    // -------------------------------------------------------------------------

    /// <summary>IDE disk/CDROM slot 0 configuration string.</summary>
    [JsonPropertyName("ide0")]
    [JsonProperty("ide0")]
    public string? Ide0 { get; set; }

    /// <summary>IDE disk/CDROM slot 1 configuration string.</summary>
    [JsonPropertyName("ide1")]
    [JsonProperty("ide1")]
    public string? Ide1 { get; set; }

    /// <summary>IDE disk/CDROM slot 2 configuration string.</summary>
    [JsonPropertyName("ide2")]
    [JsonProperty("ide2")]
    public string? Ide2 { get; set; }

    /// <summary>IDE disk/CDROM slot 3 configuration string.</summary>
    [JsonPropertyName("ide3")]
    [JsonProperty("ide3")]
    public string? Ide3 { get; set; }

    // -------------------------------------------------------------------------
    // SATA disk slots (0–5)
    // -------------------------------------------------------------------------

    /// <summary>SATA disk slot 0 configuration string.</summary>
    [JsonPropertyName("sata0")]
    [JsonProperty("sata0")]
    public string? Sata0 { get; set; }

    /// <summary>SATA disk slot 1 configuration string.</summary>
    [JsonPropertyName("sata1")]
    [JsonProperty("sata1")]
    public string? Sata1 { get; set; }

    /// <summary>SATA disk slot 2 configuration string.</summary>
    [JsonPropertyName("sata2")]
    [JsonProperty("sata2")]
    public string? Sata2 { get; set; }

    /// <summary>SATA disk slot 3 configuration string.</summary>
    [JsonPropertyName("sata3")]
    [JsonProperty("sata3")]
    public string? Sata3 { get; set; }

    /// <summary>SATA disk slot 4 configuration string.</summary>
    [JsonPropertyName("sata4")]
    [JsonProperty("sata4")]
    public string? Sata4 { get; set; }

    /// <summary>SATA disk slot 5 configuration string.</summary>
    [JsonPropertyName("sata5")]
    [JsonProperty("sata5")]
    public string? Sata5 { get; set; }

    // -------------------------------------------------------------------------
    // Network interface slots (0–7)
    // -------------------------------------------------------------------------

    /// <summary>Network interface 0 configuration string (e.g., "virtio=XX:XX:XX:XX:XX:XX,bridge=vmbr0").</summary>
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
    // Cloud-Init
    // -------------------------------------------------------------------------

    /// <summary>Cloud-Init default user name.</summary>
    [JsonPropertyName("ciuser")]
    [JsonProperty("ciuser")]
    public string? CiUser { get; set; }

    /// <summary>Cloud-Init default user password (hashed or plaintext depending on PVE version).</summary>
    [JsonPropertyName("cipassword")]
    [JsonProperty("cipassword")]
    public string? CiPassword { get; set; }

    /// <summary>URL-encoded SSH public keys injected by Cloud-Init.</summary>
    [JsonPropertyName("sshkeys")]
    [JsonProperty("sshkeys")]
    public string? SshKeys { get; set; }

    /// <summary>Cloud-Init IP configuration for interface 0.</summary>
    [JsonPropertyName("ipconfig0")]
    [JsonProperty("ipconfig0")]
    public string? IpConfig0 { get; set; }

    /// <summary>Cloud-Init IP configuration for interface 1.</summary>
    [JsonPropertyName("ipconfig1")]
    [JsonProperty("ipconfig1")]
    public string? IpConfig1 { get; set; }

    /// <summary>Cloud-Init IP configuration for interface 2.</summary>
    [JsonPropertyName("ipconfig2")]
    [JsonProperty("ipconfig2")]
    public string? IpConfig2 { get; set; }

    /// <summary>Cloud-Init IP configuration for interface 3.</summary>
    [JsonPropertyName("ipconfig3")]
    [JsonProperty("ipconfig3")]
    public string? IpConfig3 { get; set; }

    /// <summary>DNS nameserver(s) injected via Cloud-Init.</summary>
    [JsonPropertyName("nameserver")]
    [JsonProperty("nameserver")]
    public string? Nameserver { get; set; }

    /// <summary>DNS search domain injected via Cloud-Init.</summary>
    [JsonPropertyName("searchdomain")]
    [JsonProperty("searchdomain")]
    public string? Searchdomain { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var totalCores = (Cores ?? 1) * (Sockets ?? 1);
        return $"Config | CPUs: {totalCores} ({Sockets ?? 1}S x {Cores ?? 1}C) | "
             + $"Memory: {Memory?.ToString() ?? "N/A"} MB | BIOS: {Bios ?? "seabios"} | "
             + $"Machine: {Machine ?? "default"} | OS: {OsType ?? "N/A"}";
    }
}
