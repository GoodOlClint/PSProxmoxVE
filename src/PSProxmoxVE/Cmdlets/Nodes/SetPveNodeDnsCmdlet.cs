using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Updates the DNS configuration of a Proxmox VE node.</para>
    /// <para type="description">
    /// Modifies the DNS settings (search domain and nameservers) for the specified
    /// node via the /nodes/{node}/dns endpoint.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveNodeDns", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveNodeDnsCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        /// <summary>DNS search domain.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The DNS search domain.")]
        [ValidateNotNullOrEmpty]
        public string Search { get; set; } = string.Empty;

        /// <summary>Primary DNS server.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Primary DNS server address.")]
        public string? Dns1 { get; set; }

        /// <summary>Secondary DNS server.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Secondary DNS server address.")]
        public string? Dns2 { get; set; }

        /// <summary>Tertiary DNS server.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Tertiary DNS server address.")]
        public string? Dns3 { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Node, "Set PVE Node DNS"))
                return;

            var config = new Dictionary<string, string>
            {
                ["search"] = Search
            };
            if (!string.IsNullOrEmpty(Dns1)) config["dns1"] = Dns1!;
            if (!string.IsNullOrEmpty(Dns2)) config["dns2"] = Dns2!;
            if (!string.IsNullOrEmpty(Dns3)) config["dns3"] = Dns3!;

            var session = GetSession();
            var service = new NodeService();

            WriteVerbose($"Updating DNS configuration for node '{Node}'...");
            service.SetNodeDns(session, Node, config);
        }
    }
}
