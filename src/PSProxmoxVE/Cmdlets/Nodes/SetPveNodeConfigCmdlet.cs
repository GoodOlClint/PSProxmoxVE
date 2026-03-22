using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Updates the configuration of a Proxmox VE node.</para>
    /// <para type="description">
    /// Modifies node-level configuration properties such as description and wake-on-LAN
    /// settings via the /nodes/{node}/config endpoint.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveNodeConfig", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveNodeConfigCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        /// <summary>Node description/comment.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Node description or comment.")]
        public string? Description { get; set; }

        /// <summary>Wake-on-LAN MAC address or configuration.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wake-on-LAN configuration.")]
        public string? Wakeonlan { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Node, "Set PVE Node Config"))
                return;

            var config = new Dictionary<string, string>();
            if (Description != null) config["description"] = Description;
            if (!string.IsNullOrEmpty(Wakeonlan)) config["wakeonlan"] = Wakeonlan!;

            var session = GetSession();
            var service = new NodeService();

            WriteVerbose($"Updating configuration for node '{Node}'...");
            service.SetNodeConfig(session, Node, config);
        }
    }
}
