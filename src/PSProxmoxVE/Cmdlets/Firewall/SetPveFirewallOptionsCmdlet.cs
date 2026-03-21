using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.Set, "PveFirewallOptions", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public class SetPveFirewallOptionsCmdlet : PveCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The firewall level: Cluster, Node, Vm, or Container.")]
        [ValidateSet("Cluster", "Node", "Vm", "Container")]
        public string Level { get; set; } = string.Empty;

        [Parameter(Mandatory = false, HelpMessage = "The node name. Required when Level is Node, Vm, or Container.")]
        public string? Node { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The VM/Container ID. Required when Level is Vm or Container.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Enable or disable the firewall.")]
        public SwitchParameter Enable { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Default policy for incoming traffic: ACCEPT, DROP, or REJECT.")]
        [ValidateSet("ACCEPT", "DROP", "REJECT")]
        public string? PolicyIn { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Default policy for outgoing traffic: ACCEPT, DROP, or REJECT.")]
        [ValidateSet("ACCEPT", "DROP", "REJECT")]
        public string? PolicyOut { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Log level for incoming traffic.")]
        public string? LogLevelIn { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Log level for outgoing traffic.")]
        public string? LogLevelOut { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Enable DHCP.")]
        public SwitchParameter Dhcp { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Enable NDP.")]
        public SwitchParameter Ndp { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Enable MAC address filter.")]
        public SwitchParameter MacFilter { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Enable IP filter.")]
        public SwitchParameter IpFilter { get; set; }

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

            if (!ShouldProcess($"firewall options ({Level})", "Update"))
                return;

            var session = GetSession();
            var service = new FirewallService();
            int? vmid = VmId > 0 ? VmId : (int?)null;

            var config = new Dictionary<string, string>();

            if (Enable.IsPresent)
                config["enable"] = "1";
            if (!string.IsNullOrEmpty(PolicyIn))
                config["policy_in"] = PolicyIn!;
            if (!string.IsNullOrEmpty(PolicyOut))
                config["policy_out"] = PolicyOut!;
            if (!string.IsNullOrEmpty(LogLevelIn))
                config["log_level_in"] = LogLevelIn!;
            if (!string.IsNullOrEmpty(LogLevelOut))
                config["log_level_out"] = LogLevelOut!;
            if (Dhcp.IsPresent)
                config["dhcp"] = "1";
            if (Ndp.IsPresent)
                config["ndp"] = "1";
            if (MacFilter.IsPresent)
                config["macfilter"] = "1";
            if (IpFilter.IsPresent)
                config["ipfilter"] = "1";

            WriteVerbose($"Setting firewall options at level '{level}'...");
            service.SetOptions(session, level, config, Node, vmid);
        }
    }
}
