using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms
{
    /// <summary>
    /// Represents the Cloud-Init specific configuration fields for a VM.
    /// Fields are extracted from the full VM config.
    /// </summary>
    public class PveCloudInitConfig
    {
        /// <summary>Default user name injected by Cloud-Init.</summary>
        [JsonProperty("ciuser")]
        public string? CiUser { get; set; }

        /// <summary>Default user password (may be hashed).</summary>
        [JsonProperty("cipassword")]
        public string? CiPassword { get; set; }

        /// <summary>URL-encoded SSH public keys.</summary>
        [JsonProperty("sshkeys")]
        public string? SshKeys { get; set; }

        /// <summary>IP configuration for interface 0 (e.g. "ip=dhcp" or "ip=192.168.1.10/24,gw=192.168.1.1").</summary>
        [JsonProperty("ipconfig0")]
        public string? IpConfig0 { get; set; }

        /// <summary>IP configuration for interface 1.</summary>
        [JsonProperty("ipconfig1")]
        public string? IpConfig1 { get; set; }

        /// <summary>IP configuration for interface 2.</summary>
        [JsonProperty("ipconfig2")]
        public string? IpConfig2 { get; set; }

        /// <summary>IP configuration for interface 3.</summary>
        [JsonProperty("ipconfig3")]
        public string? IpConfig3 { get; set; }

        /// <summary>DNS nameserver(s) space-separated.</summary>
        [JsonProperty("nameserver")]
        public string? Nameserver { get; set; }

        /// <summary>DNS search domain.</summary>
        [JsonProperty("searchdomain")]
        public string? Searchdomain { get; set; }

        /// <summary>Custom Cloud-Init config files (cicustom field).</summary>
        [JsonProperty("cicustom")]
        public string? CiCustom { get; set; }

        /// <inheritdoc />
        public override string ToString() =>
            $"CloudInit: User={CiUser ?? "N/A"} | IP0={IpConfig0 ?? "N/A"} | NS={Nameserver ?? "N/A"}";
    }
}
