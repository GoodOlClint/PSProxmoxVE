using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Updates an SDN controller configuration in Proxmox VE.</para>
    /// <para type="description">
    /// Modifies the specified Software-Defined Networking controller configuration.
    /// Changes are pending until Invoke-PveSdnApply is called.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveSdnController", SupportsShouldProcess = true)]
    public class SetPveSdnControllerCmdlet : PveCmdletBase
    {
        /// <summary>The controller identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The SDN controller identifier.")]
        public string Controller { get; set; } = string.Empty;

        /// <summary>The Autonomous System Number for BGP/EVPN.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The Autonomous System Number for BGP/EVPN.")]
        public int? Asn { get; set; }

        /// <summary>Comma-separated list of BGP peer addresses.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of BGP peer addresses.")]
        public string? Peers { get; set; }

        /// <summary>The node this controller is configured on.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The node this controller is configured on.")]
        public string? Node { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Controller, "Set PVE SDN Controller"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);
            var service = new NetworkService();

            WriteVerbose($"Updating SDN controller '{Controller}'...");
            var config = new Dictionary<string, string>();

            if (MyInvocation.BoundParameters.ContainsKey("Asn"))
                config["asn"] = Asn!.Value.ToString();
            if (!string.IsNullOrEmpty(Peers)) config["peers"] = Peers!;
            if (!string.IsNullOrEmpty(Node))  config["node"]  = Node!;

            service.UpdateSdnController(session, Controller, config);
        }
    }
}
