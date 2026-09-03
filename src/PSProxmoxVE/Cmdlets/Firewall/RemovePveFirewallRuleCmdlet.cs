using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.Remove, "PveFirewallRule", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveFirewallRuleCmdlet : PveCmdletBase
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

        [Parameter(Mandatory = true, HelpMessage = "The rule position to remove.")]
        public int Position { get; set; }

        protected override void ProcessPveRecord()
        {
            var level = Level;
            if (!FirewallScope.TryValidate(level, Node, VmId, Group, out var scopeErrorId, out var scopeMessage))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new PSArgumentException(scopeMessage), scopeErrorId, ErrorCategory.InvalidArgument, null));
                return;
            }

            var target = string.Equals(level, "Group", StringComparison.OrdinalIgnoreCase)
                ? $"firewall rule at position {Position} ({Level} '{Group}')"
                : $"firewall rule at position {Position} ({Level})";
            if (!ShouldProcess(target, "Remove"))
                return;

            var session = GetSession();
            var service = new FirewallService();
            var vmid = VmId;

            WriteVerbose($"Removing firewall rule at position {Position} ({level})...");
            if (string.Equals(level, "Group", StringComparison.OrdinalIgnoreCase))
                service.RemoveGroupRule(session, Group!, Position);
            else
                service.RemoveRule(session, level, Position, Node, vmid);
        }
    }
}
