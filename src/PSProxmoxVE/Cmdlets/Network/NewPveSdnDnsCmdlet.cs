using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Creates a new SDN DNS plugin in Proxmox VE.</para>
    /// <para type="description">
    /// Adds a new Software-Defined Networking DNS plugin.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveSdnDns", SupportsShouldProcess = true)]
    public class NewPveSdnDnsCmdlet : PveCmdletBase
    {
        /// <summary>The DNS plugin identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The DNS plugin identifier.")]
        public string Dns { get; set; } = string.Empty;

        /// <summary>The DNS plugin type.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The DNS plugin type.")]
        [ValidateSet("powerdns")]
        public string Type { get; set; } = string.Empty;

        /// <summary>The URL of the DNS service API.</summary>
        [Parameter(Mandatory = true, HelpMessage = "The URL of the DNS service API.")]
        public string Url { get; set; } = string.Empty;

        /// <summary>The API key for the DNS service.</summary>
        [Parameter(Mandatory = true, HelpMessage = "The API key for the DNS service.")]
        public string Key { get; set; } = string.Empty;

        /// <summary>The IPv6 reverse zone mask length.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The IPv6 reverse zone mask length.")]
        public int? ReverseMaskV6 { get; set; }

        /// <summary>The TTL (time-to-live) for DNS records.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The TTL (time-to-live) for DNS records.")]
        public int? Ttl { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"SDN DNS '{Dns}'", "Create PVE SDN DNS"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN IPAM/DNS/Controller", 6, 2, 8, 1);
            var service = new NetworkService();

            WriteVerbose($"Creating SDN DNS plugin '{Dns}' of type '{Type}'...");
            var data = new Dictionary<string, string>
            {
                ["dns"] = Dns,
                ["type"] = Type,
                ["url"] = Url,
                ["key"] = Key
            };

            if (ReverseMaskV6.HasValue) data["reversemaskv6"] = ReverseMaskV6.Value.ToString();
            if (Ttl.HasValue)           data["ttl"]           = Ttl.Value.ToString();

            service.CreateSdnDnsPlugin(session, data);
        }
    }
}
