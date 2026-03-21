using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.Get, "PveFirewallOptions")]
    [OutputType(typeof(PveFirewallOptions))]
    public class GetPveFirewallOptionsCmdlet : PveCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The firewall level: Cluster, Node, Vm, or Container.")]
        [ValidateSet("Cluster", "Node", "Vm", "Container")]
        public string Level { get; set; } = string.Empty;

        [Parameter(Mandatory = false, HelpMessage = "The node name. Required when Level is Node, Vm, or Container.")]
        public string? Node { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The VM/Container ID. Required when Level is Vm or Container.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        protected override void ProcessRecord()
        {
            var level = Level;
            if (!string.Equals(level, "Cluster", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(Node))
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new PSArgumentException("Node is required when Level is not Cluster."),
                        "NodeRequired", ErrorCategory.InvalidArgument, null));
                    return;
                }
            }
            if (string.Equals(level, "Vm", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(level, "Container", StringComparison.OrdinalIgnoreCase))
            {
                if (VmId == 0)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new PSArgumentException("VmId is required when Level is Vm or Container."),
                        "VmIdRequired", ErrorCategory.InvalidArgument, null));
                    return;
                }
            }

            var session = GetSession();
            var service = new FirewallService();
            int? vmid = VmId > 0 ? VmId : (int?)null;

            WriteVerbose($"Getting firewall options at level '{level}'...");
            var options = service.GetOptions(session, level, Node, vmid);
            WriteObject(options);
        }
    }
}
