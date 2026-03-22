using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Updates an SDN IPAM plugin configuration in Proxmox VE.</para>
    /// <para type="description">
    /// Modifies the specified Software-Defined Networking IPAM plugin configuration.
    /// Changes are pending until Invoke-PveSdnApply is called.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveSdnIpam", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveSdnIpamCmdlet : PveCmdletBase
    {
        /// <summary>The IPAM plugin identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The SDN IPAM plugin identifier.")]
        public string Ipam { get; set; } = string.Empty;

        /// <summary>The IPAM server URL.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The IPAM server URL.")]
        public string? Url { get; set; }

        /// <summary>The API token for the IPAM server.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The API token for the IPAM server.")]
        public string? Token { get; set; }

        /// <summary>The section ID in the IPAM server.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The section ID in the IPAM server.")]
        public int? Section { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Ipam, "Set PVE SDN IPAM"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);
            var service = new NetworkService();

            WriteVerbose($"Updating SDN IPAM plugin '{Ipam}'...");
            var config = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(Url))   config["url"]   = Url!;
            if (!string.IsNullOrEmpty(Token)) config["token"] = Token!;
            if (MyInvocation.BoundParameters.ContainsKey("Section"))
                config["section"] = Section!.Value.ToString();

            service.UpdateSdnIpam(session, Ipam, config);
        }
    }
}
