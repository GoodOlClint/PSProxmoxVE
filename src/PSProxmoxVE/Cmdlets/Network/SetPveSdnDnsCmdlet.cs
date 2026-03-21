using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Updates an SDN DNS plugin configuration in Proxmox VE.</para>
    /// <para type="description">
    /// Modifies the specified Software-Defined Networking DNS plugin configuration.
    /// Changes are pending until Invoke-PveSdnApply is called.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveSdnDns", SupportsShouldProcess = true)]
    public class SetPveSdnDnsCmdlet : PveCmdletBase
    {
        /// <summary>The DNS plugin identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The SDN DNS plugin identifier.")]
        public string Dns { get; set; } = string.Empty;

        /// <summary>The DNS server URL.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The DNS server URL.")]
        public string? Url { get; set; }

        /// <summary>The TSIG key for DNS updates.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The TSIG key for DNS updates.")]
        public string? Key { get; set; }

        /// <summary>The IPv6 reverse DNS mask length.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The IPv6 reverse DNS mask length.")]
        public int? ReverseMaskV6 { get; set; }

        /// <summary>The TTL for DNS records.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The TTL for DNS records.")]
        public int? Ttl { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Dns, "Set PVE SDN DNS"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);
            var service = new NetworkService();

            WriteVerbose($"Updating SDN DNS plugin '{Dns}'...");
            var config = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(Url)) config["url"] = Url!;
            if (!string.IsNullOrEmpty(Key)) config["key"] = Key!;
            if (MyInvocation.BoundParameters.ContainsKey("ReverseMaskV6"))
                config["reversemaskv6"] = ReverseMaskV6!.Value.ToString();
            if (MyInvocation.BoundParameters.ContainsKey("Ttl"))
                config["ttl"] = Ttl!.Value.ToString();

            service.UpdateSdnDns(session, Dns, config);
        }
    }
}
