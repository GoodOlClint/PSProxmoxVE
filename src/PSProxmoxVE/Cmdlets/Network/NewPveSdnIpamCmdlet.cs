using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Creates a new SDN IPAM plugin in Proxmox VE.</para>
    /// <para type="description">
    /// Adds a new Software-Defined Networking IPAM plugin.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveSdnIpam", SupportsShouldProcess = true)]
    public class NewPveSdnIpamCmdlet : PveCmdletBase
    {
        /// <summary>The IPAM plugin identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The IPAM plugin identifier.")]
        public string Ipam { get; set; } = string.Empty;

        /// <summary>The IPAM plugin type.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The IPAM plugin type.")]
        [ValidateSet("pve", "netbox", "phpipam")]
        public string Type { get; set; } = string.Empty;

        /// <summary>The URL of the external IPAM service.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The URL of the external IPAM service.")]
        public string? Url { get; set; }

        /// <summary>The API token for the external IPAM service.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The API token for the external IPAM service.")]
        public string? Token { get; set; }

        /// <summary>The configuration section identifier.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The configuration section identifier.")]
        public int? Section { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"SDN IPAM '{Ipam}'", "Create PVE SDN IPAM"))
                return;

            var session = GetSession();
            var service = new NetworkService();

            WriteVerbose($"Creating SDN IPAM plugin '{Ipam}' of type '{Type}'...");
            var data = new Dictionary<string, string>
            {
                ["ipam"] = Ipam,
                ["type"] = Type
            };

            if (!string.IsNullOrEmpty(Url))    data["url"]     = Url!;
            if (!string.IsNullOrEmpty(Token))   data["token"]   = Token!;
            if (Section.HasValue)               data["section"] = Section.Value.ToString();

            service.CreateSdnIpam(session, data);
        }
    }
}
