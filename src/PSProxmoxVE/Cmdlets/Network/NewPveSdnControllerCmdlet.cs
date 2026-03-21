using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Creates a new SDN controller in Proxmox VE.</para>
    /// <para type="description">
    /// Adds a new Software-Defined Networking controller.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveSdnController", SupportsShouldProcess = true)]
    public class NewPveSdnControllerCmdlet : PveCmdletBase
    {
        /// <summary>The controller identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The controller identifier.")]
        public string Controller { get; set; } = string.Empty;

        /// <summary>The controller type.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The controller type.")]
        [ValidateSet("evpn", "bgp")]
        public string Type { get; set; } = string.Empty;

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
            if (!ShouldProcess($"SDN Controller '{Controller}'", "Create PVE SDN Controller"))
                return;

            var session = GetSession();
            var service = new NetworkService();

            WriteVerbose($"Creating SDN controller '{Controller}' of type '{Type}'...");
            var data = new Dictionary<string, string>
            {
                ["controller"] = Controller,
                ["type"] = Type
            };

            if (Asn.HasValue)                   data["asn"]   = Asn.Value.ToString();
            if (!string.IsNullOrEmpty(Peers))    data["peers"] = Peers!;
            if (!string.IsNullOrEmpty(Node))     data["node"]  = Node!;

            service.CreateSdnController(session, data);
        }
    }
}
