using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Stops all VMs and containers on a Proxmox VE node.</para>
    /// <para type="description">
    /// Triggers the stop-all operation on the specified node, which stops all running VMs
    /// and containers. Optionally limit to specific VM IDs or force-stop.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "PveNodeVms",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class StopPveNodeVmsCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        /// <summary>Comma-separated list of VM IDs to stop. If omitted, all VMs are stopped.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of VM IDs to stop.")]
        public string? VmIds { get; set; }

        /// <summary>Force stop VMs (do not wait for graceful shutdown).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Force stop VMs without waiting for graceful shutdown.")]
        public SwitchParameter ForceStop { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Node, "Stop all VMs"))
                return;

            var config = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(VmIds)) config["vms"] = VmIds!;
            if (ForceStop.IsPresent) config["force-stop"] = "1";

            var session = GetSession();
            var service = new NodeService();

            WriteVerbose($"Stopping all VMs on node '{Node}'...");
            var task = service.StopAll(session, Node, config);

            WriteObject(task);
        }
    }
}
