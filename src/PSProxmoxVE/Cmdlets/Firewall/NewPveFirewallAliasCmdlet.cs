using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.New, "PveFirewallAlias", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class NewPveFirewallAliasCmdlet : PveCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The firewall level: Cluster, Node, Vm, or Container.")]
        [ValidateSet("Cluster", "Node", "Vm", "Container")]
        public string Level { get; set; } = string.Empty;

        [Parameter(Mandatory = false, HelpMessage = "The node name. Required when Level is Node, Vm, or Container.")]
        public string? Node { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The VM/Container ID. Required when Level is Vm or Container.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "The alias name.")]
        public string Name { get; set; } = string.Empty;

        [Parameter(Mandatory = true, HelpMessage = "The CIDR network address.")]
        public string Cidr { get; set; } = string.Empty;

        [Parameter(Mandatory = false, HelpMessage = "Optional comment for the alias.")]
        public string? Comment { get; set; }

        protected override void ProcessPveRecord()
        {
            var level = Level;
            if (!FirewallScope.TryValidate(level, Node, VmId, null, out var scopeErrorId, out var scopeMessage))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new PSArgumentException(scopeMessage), scopeErrorId, ErrorCategory.InvalidArgument, null));
                return;
            }

            if (!ShouldProcess($"firewall alias '{Name}' ({Level})", "Create"))
                return;

            var session = GetSession();
            var service = new FirewallService();
            var vmid = VmId;

            WriteVerbose($"Creating firewall alias '{Name}' at level '{level}'...");
            service.CreateAlias(session, level, Name, Cidr, Comment, Node, vmid);
        }
    }
}
