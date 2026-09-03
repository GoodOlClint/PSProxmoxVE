using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.Remove, "PveFirewallIpSetEntry", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveFirewallIpSetEntryCmdlet : PveCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The firewall level: Cluster, Node, Vm, or Container.")]
        [ValidateSet("Cluster", "Node", "Vm", "Container")]
        public string Level { get; set; } = string.Empty;

        [Parameter(Mandatory = false, HelpMessage = "The node name. Required when Level is Node, Vm, or Container.")]
        public string? Node { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The VM/Container ID. Required when Level is Vm or Container.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The IP set name.")]
        public string Name { get; set; } = string.Empty;

        [Parameter(Mandatory = true, HelpMessage = "The CIDR network address to remove.")]
        public string Cidr { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            var level = Level;
            if (!FirewallScope.TryValidate(level, Node, VmId, null, out var scopeErrorId, out var scopeMessage))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new PSArgumentException(scopeMessage), scopeErrorId, ErrorCategory.InvalidArgument, null));
                return;
            }

            if (!ShouldProcess($"IP set entry '{Cidr}' from '{Name}' ({Level})", "Remove"))
                return;

            var session = GetSession();
            var service = new FirewallService();
            var vmid = VmId;

            WriteVerbose($"Removing entry '{Cidr}' from IP set '{Name}' at level '{level}'...");
            service.RemoveIpSetEntry(session, level, Name, Cidr, Node, vmid);
        }
    }
}
