using System;
using System.Linq;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.Get, "PveFirewallRule")]
    [OutputType(typeof(PveFirewallRule))]
    public sealed class GetPveFirewallRuleCmdlet : PveCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The firewall level: Cluster, Node, Vm, Container, or Group.")]
        [ValidateSet("Cluster", "Node", "Vm", "Container", "Group")]
        public string Level { get; set; } = string.Empty;

        [Parameter(Mandatory = false, HelpMessage = "The node name. Required when Level is Node, Vm, or Container.")]
        public string? Node { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The VM/Container ID. Required when Level is Vm or Container.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The security group name. Required when Level is Group.")]
        public string? Group { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Optional rule position to filter by.")]
        public int? Position { get; set; }

        protected override void ProcessRecord()
        {
            var level = Level;
            if (!string.Equals(level, "Cluster", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(level, "Group", StringComparison.OrdinalIgnoreCase))
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
                if (!VmId.HasValue)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new PSArgumentException("VmId is required when Level is Vm or Container."),
                        "VmIdRequired", ErrorCategory.InvalidArgument, null));
                    return;
                }
            }
            if (string.Equals(level, "Group", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(Group))
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new PSArgumentException("Group is required when Level is Group."),
                        "GroupRequired", ErrorCategory.InvalidArgument, null));
                    return;
                }
            }

            var session = GetSession();
            var service = new FirewallService();
            var vmid = VmId;

            WriteVerbose($"Getting firewall rules at level '{level}'...");
            var rules = string.Equals(level, "Group", StringComparison.OrdinalIgnoreCase)
                ? service.GetGroupRules(session, Group!)
                : service.GetRules(session, level, Node, vmid);

            if (Position.HasValue)
            {
                var filtered = rules.Where(r => r.Pos == Position.Value).ToArray();
                WriteObject(filtered, true);
            }
            else
            {
                WriteObject(rules, true);
            }
        }
    }
}
