using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Utilities;

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
    [JsonProperty("cores")]
    public int? Cores { get; set; }

    /// <summary>
    /// Number of CPU sockets.
    /// </summary>
    [JsonProperty("sockets")]
    public int? Sockets { get; set; }

    /// <summary>
    /// Memory size in megabytes.
    /// </summary>
    [JsonProperty("memory")]
    public int? Memory { get; set; }

    /// <summary>
    /// Emulated CPU type (e.g., "host", "x86-64-v2-AES").
    /// </summary>
    [JsonProperty("cpu")]
    public string? CpuType { get; set; }

    // -------------------------------------------------------------------------
    // Firmware / Machine
    // -------------------------------------------------------------------------

    /// <summary>
    /// BIOS implementation to use: "seabios" (default) or "ovmf" (UEFI).
    /// </summary>
    [JsonProperty("bios")]
    public string? Bios { get; set; }

    /// <summary>
    /// Emulated machine type (e.g., "q35", "i440fx").
    /// </summary>
    [JsonProperty("machine")]
    public string? Machine { get; set; }

    /// <summary>
    /// SCSI controller hardware model (e.g., "virtio-scsi-single", "lsi").
    /// </summary>
    [JsonProperty("scsihw")]
    public string? ScsiHardware { get; set; }

    /// <summary>
    /// EFI vars disk spec (present on OVMF/UEFI VMs), e.g. "local-lvm:vm-100-disk-1,...".
    /// </summary>
    [JsonProperty("efidisk0")]
    public string? EfiDisk0 { get; set; }

    /// <summary>
    /// TPM state disk spec (present on VMs with a virtual TPM).
    /// </summary>
    [JsonProperty("tpmstate0")]
    public string? TpmState0 { get; set; }

    // -------------------------------------------------------------------------
    // Boot / Args
    // -------------------------------------------------------------------------

    /// <summary>
    /// Boot order specification string.
    /// </summary>
    [JsonProperty("boot")]
    public string? Boot { get; set; }

    /// <summary>
    /// Arbitrary QEMU command-line arguments appended to the QEMU launch command.
    /// </summary>
    [JsonProperty("args")]
    public string? Args { get; set; }

    // -------------------------------------------------------------------------
    // Metadata
    // -------------------------------------------------------------------------

    /// <summary>
    /// Human-readable description or notes for the VM.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Semicolon-separated list of tags assigned to the VM.
    /// </summary>
    [JsonProperty("tags")]
    public string? Tags { get; set; }

    /// <summary>
    /// When set to 1, prevents the VM from being deleted or modified accidentally.
    /// </summary>
    [JsonProperty("protection")]
    public int? Protection { get; set; }

    /// <summary>
    /// NUMA topology enabled (1) or disabled (0).
    /// </summary>
    [JsonProperty("numa")]
    public int? Numa { get; set; }

    /// <summary>
    /// VirtIO balloon device target memory in MB. 0 disables ballooning.
    /// </summary>
    [JsonProperty("balloon")]
    public int? Balloon { get; set; }

    /// <summary>
    /// Guest OS type hint (e.g., "l26" for Linux 2.6+, "win10").
    /// </summary>
    [JsonProperty("ostype")]
    public string? OsType { get; set; }

    // -------------------------------------------------------------------------
    // VirtIO disk slots (0–3, most commonly used)
    // -------------------------------------------------------------------------

    /// <summary>VirtIO disk slot 0 configuration string.</summary>
    [JsonProperty("virtio0")]
    public string? Virtio0 { get; set; }

    /// <summary>VirtIO disk slot 1 configuration string.</summary>
    [JsonProperty("virtio1")]
    public string? Virtio1 { get; set; }

    /// <summary>VirtIO disk slot 2 configuration string.</summary>
    [JsonProperty("virtio2")]
    public string? Virtio2 { get; set; }

    /// <summary>VirtIO disk slot 3 configuration string.</summary>
    [JsonProperty("virtio3")]
    public string? Virtio3 { get; set; }

    // -------------------------------------------------------------------------
    // SCSI disk slots (0–7)
    // -------------------------------------------------------------------------

    /// <summary>SCSI disk slot 0 configuration string.</summary>
    [JsonProperty("scsi0")]
    public string? Scsi0 { get; set; }

    /// <summary>SCSI disk slot 1 configuration string.</summary>
    [JsonProperty("scsi1")]
    public string? Scsi1 { get; set; }

    /// <summary>SCSI disk slot 2 configuration string.</summary>
    [JsonProperty("scsi2")]
    public string? Scsi2 { get; set; }

    /// <summary>SCSI disk slot 3 configuration string.</summary>
    [JsonProperty("scsi3")]
    public string? Scsi3 { get; set; }

    /// <summary>SCSI disk slot 4 configuration string.</summary>
    [JsonProperty("scsi4")]
    public string? Scsi4 { get; set; }

    /// <summary>SCSI disk slot 5 configuration string.</summary>
    [JsonProperty("scsi5")]
    public string? Scsi5 { get; set; }

    /// <summary>SCSI disk slot 6 configuration string.</summary>
    [JsonProperty("scsi6")]
    public string? Scsi6 { get; set; }

    /// <summary>SCSI disk slot 7 configuration string.</summary>
    [JsonProperty("scsi7")]
    public string? Scsi7 { get; set; }

    // -------------------------------------------------------------------------
    // IDE disk slots (0–3)
    // -------------------------------------------------------------------------

    /// <summary>IDE disk/CDROM slot 0 configuration string.</summary>
    [JsonProperty("ide0")]
    public string? Ide0 { get; set; }

    /// <summary>IDE disk/CDROM slot 1 configuration string.</summary>
    [JsonProperty("ide1")]
    public string? Ide1 { get; set; }

    /// <summary>IDE disk/CDROM slot 2 configuration string.</summary>
    [JsonProperty("ide2")]
    public string? Ide2 { get; set; }

    /// <summary>IDE disk/CDROM slot 3 configuration string.</summary>
    [JsonProperty("ide3")]
    public string? Ide3 { get; set; }

    // -------------------------------------------------------------------------
    // SATA disk slots (0–5)
    // -------------------------------------------------------------------------

    /// <summary>SATA disk slot 0 configuration string.</summary>
    [JsonProperty("sata0")]
    public string? Sata0 { get; set; }

    /// <summary>SATA disk slot 1 configuration string.</summary>
    [JsonProperty("sata1")]
    public string? Sata1 { get; set; }

    /// <summary>SATA disk slot 2 configuration string.</summary>
    [JsonProperty("sata2")]
    public string? Sata2 { get; set; }

    /// <summary>SATA disk slot 3 configuration string.</summary>
    [JsonProperty("sata3")]
    public string? Sata3 { get; set; }

    /// <summary>SATA disk slot 4 configuration string.</summary>
    [JsonProperty("sata4")]
    public string? Sata4 { get; set; }

    /// <summary>SATA disk slot 5 configuration string.</summary>
    [JsonProperty("sata5")]
    public string? Sata5 { get; set; }

    // -------------------------------------------------------------------------
    // Network interface slots (0–7)
    // -------------------------------------------------------------------------

    /// <summary>Network interface 0 configuration string (e.g., "virtio=XX:XX:XX:XX:XX:XX,bridge=vmbr0").</summary>
    [JsonProperty("net0")]
    public string? Net0 { get; set; }

    /// <summary>Network interface 1 configuration string.</summary>
    [JsonProperty("net1")]
    public string? Net1 { get; set; }

    /// <summary>Network interface 2 configuration string.</summary>
    [JsonProperty("net2")]
    public string? Net2 { get; set; }

    /// <summary>Network interface 3 configuration string.</summary>
    [JsonProperty("net3")]
    public string? Net3 { get; set; }

    /// <summary>Network interface 4 configuration string.</summary>
    [JsonProperty("net4")]
    public string? Net4 { get; set; }

    /// <summary>Network interface 5 configuration string.</summary>
    [JsonProperty("net5")]
    public string? Net5 { get; set; }

    /// <summary>Network interface 6 configuration string.</summary>
    [JsonProperty("net6")]
    public string? Net6 { get; set; }

    /// <summary>Network interface 7 configuration string.</summary>
    [JsonProperty("net7")]
    public string? Net7 { get; set; }

    // -------------------------------------------------------------------------
    // Cloud-Init
    // -------------------------------------------------------------------------

    /// <summary>Cloud-Init default user name.</summary>
    [JsonProperty("ciuser")]
    public string? CiUser { get; set; }

    /// <summary>Cloud-Init default user password (hashed or plaintext depending on PVE version).</summary>
    [JsonProperty("cipassword")]
    public string? CiPassword { get; set; }

    /// <summary>URL-encoded SSH public keys injected by Cloud-Init.</summary>
    [JsonProperty("sshkeys")]
    public string? SshKeys { get; set; }

    /// <summary>Cloud-Init IP configuration for interface 0.</summary>
    [JsonProperty("ipconfig0")]
    public string? IpConfig0 { get; set; }

    /// <summary>Cloud-Init IP configuration for interface 1.</summary>
    [JsonProperty("ipconfig1")]
    public string? IpConfig1 { get; set; }

    /// <summary>Cloud-Init IP configuration for interface 2.</summary>
    [JsonProperty("ipconfig2")]
    public string? IpConfig2 { get; set; }

    /// <summary>Cloud-Init IP configuration for interface 3.</summary>
    [JsonProperty("ipconfig3")]
    public string? IpConfig3 { get; set; }

    /// <summary>DNS nameserver(s) injected via Cloud-Init.</summary>
    [JsonProperty("nameserver")]
    public string? Nameserver { get; set; }

    /// <summary>DNS search domain injected via Cloud-Init.</summary>
    [JsonProperty("searchdomain")]
    public string? Searchdomain { get; set; }

    // -------------------------------------------------------------------------
    // Catch-all for unmapped config keys
    // -------------------------------------------------------------------------

    /// <summary>
    /// Raw landing spot for any config key not mapped to a typed property above.
    /// Populated by Newtonsoft during deserialization; exposed natively via
    /// <see cref="AdditionalProperties"/>.
    /// </summary>
    [JsonExtensionData]
    private IDictionary<string, JToken>? ExtensionData { get; set; }

    private Dictionary<string, object?>? _additionalProperties;

    /// <summary>
    /// Any VM config keys not surfaced as a typed property above (e.g. hostpci0,
    /// usb0, numa0, additional disk buses). Keys map to native .NET values so the
    /// dictionary works naturally in PowerShell pipelines.
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object?> AdditionalProperties =>
        // Built once from the deserialized extension data (the model is effectively
        // immutable after deserialization), avoiding a fresh allocation per access
        // when iterating many configs in a pipeline.
        _additionalProperties ??= ExtensionData == null
            ? new Dictionary<string, object?>()
            : ExtensionData.ToDictionary(kvp => kvp.Key, kvp => JsonHelper.ToNative(kvp.Value));

    /// <inheritdoc />
    public override string ToString()
    {
        var totalCores = (Cores ?? 1) * (Sockets ?? 1);
        return $"Config | CPUs: {totalCores} ({Sockets ?? 1}S x {Cores ?? 1}C) | "
             + $"Memory: {Memory?.ToString() ?? "N/A"} MB | BIOS: {Bios ?? "seabios"} | "
             + $"Machine: {Machine ?? "default"} | OS: {OsType ?? "N/A"}";
    }
}
