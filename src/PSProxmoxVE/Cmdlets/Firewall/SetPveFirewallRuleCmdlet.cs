using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.Set, "PveFirewallRule", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveFirewallRuleCmdlet : PveCmdletBase
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

        [Parameter(Mandatory = true, HelpMessage = "The rule position to update.")]
        public int Position { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The rule type: in, out, or group.")]
        [ValidateSet("in", "out", "group")]
        public string? Type { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The rule action: ACCEPT, DROP, or REJECT.")]
        [ValidateSet("ACCEPT", "DROP", "REJECT")]
        public string? Action { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Enable the rule.")]
        public SwitchParameter Enable { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Source address or alias.")]
        public string? Source { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Destination address or alias.")]
        public string? Dest { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Protocol (e.g., tcp, udp, icmp).")]
        public string? Proto { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Destination port.")]
        public string? Dport { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Source port.")]
        public string? Sport { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Rule comment.")]
        public string? Comment { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Macro name.")]
        public string? Macro { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Log level.")]
        public string? Log { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Network interface.")]
        public string? Iface { get; set; }

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
            if (!ShouldProcess(target, "Update"))
                return;

            var session = GetSession();
            var service = new FirewallService();
            var vmid = VmId;

            var config = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(Type))
                config["type"] = Type!;
            if (!string.IsNullOrEmpty(Action))
                config["action"] = Action!;
            if (Enable.IsPresent)
                config["enable"] = "1";
            if (!string.IsNullOrEmpty(Source))
                config["source"] = Source!;
            if (!string.IsNullOrEmpty(Dest))
                config["dest"] = Dest!;
            if (!string.IsNullOrEmpty(Proto))
                config["proto"] = Proto!;
            if (!string.IsNullOrEmpty(Dport))
                config["dport"] = Dport!;
            if (!string.IsNullOrEmpty(Sport))
                config["sport"] = Sport!;
            if (!string.IsNullOrEmpty(Comment))
                config["comment"] = Comment!;
            if (!string.IsNullOrEmpty(Macro))
                config["macro"] = Macro!;
            if (!string.IsNullOrEmpty(Log))
                config["log"] = Log!;
            if (!string.IsNullOrEmpty(Iface))
                config["iface"] = Iface!;

            WriteVerbose($"Updating firewall rule at position {Position} ({level})...");
            if (string.Equals(level, "Group", StringComparison.OrdinalIgnoreCase))
                service.UpdateGroupRule(session, Group!, Position, config);
            else
                service.UpdateRule(session, level, Position, config, Node, vmid);
        }
    }
}
