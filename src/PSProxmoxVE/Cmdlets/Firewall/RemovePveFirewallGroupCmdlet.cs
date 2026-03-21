using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.Remove, "PveFirewallGroup", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public class RemovePveFirewallGroupCmdlet : PveCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The security group name to remove.")]
        public string Group { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"firewall security group '{Group}'", "Remove"))
                return;

            var session = GetSession();
            var service = new FirewallService();

            WriteVerbose($"Removing firewall security group '{Group}'...");
            service.RemoveGroup(session, Group);
        }
    }
}
