using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Starts all VMs and containers on a Proxmox VE node.</para>
    /// <para type="description">
    /// Triggers the start-all operation on the specified node, which starts all VMs and
    /// containers that are configured to auto-start. Optionally limit to specific VM IDs.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "PveNodeVms", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class StartPveNodeVmsCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        /// <summary>Comma-separated list of VM IDs to start. If omitted, all VMs are started.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of VM IDs to start.")]
        public string? VmIds { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Node, "Start all VMs"))
                return;

            var config = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(VmIds)) config["vms"] = VmIds!;

            var session = GetSession();
            var service = new NodeService();

            WriteVerbose($"Starting all VMs on node '{Node}'...");
            var task = service.StartAll(session, Node, config);

            WriteObject(task);
        }
    }
}
