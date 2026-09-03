using System;
using System.Linq;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.Get, "PveFirewallIpSet")]
    [OutputType(typeof(PveFirewallIpSet))]
    public sealed class GetPveFirewallIpSetCmdlet : PveCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The firewall level: Cluster, Node, Vm, or Container.")]
        [ValidateSet("Cluster", "Node", "Vm", "Container")]
        public string Level { get; set; } = string.Empty;

        [Parameter(Mandatory = false, HelpMessage = "The node name. Required when Level is Node, Vm, or Container.")]
        public string? Node { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The VM/Container ID. Required when Level is Vm or Container.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Optional IP set name to filter by.")]
        public string? Name { get; set; }

        protected override void ProcessPveRecord()
        {
            var level = Level;
            if (!FirewallScope.TryValidate(level, Node, VmId, null, out var scopeErrorId, out var scopeMessage))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new PSArgumentException(scopeMessage), scopeErrorId, ErrorCategory.InvalidArgument, null));
                return;
            }

            var session = GetSession();
            var service = new FirewallService();
            var vmid = VmId;

            WriteVerbose($"Getting firewall IP sets at level '{level}'...");
            var ipSets = service.GetIpSets(session, level, Node, vmid);

            if (!string.IsNullOrEmpty(Name))
            {
                var filtered = ipSets.Where(s => string.Equals(s.Name, Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                WriteObject(filtered, true);
            }
            else
            {
                WriteObject(ipSets, true);
            }
        }
    }
}
