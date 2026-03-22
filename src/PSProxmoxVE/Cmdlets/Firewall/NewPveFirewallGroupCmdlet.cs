using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.New, "PveFirewallGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class NewPveFirewallGroupCmdlet : PveCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The security group name.")]
        public string Group { get; set; } = string.Empty;

        [Parameter(Mandatory = false, HelpMessage = "Optional comment for the security group.")]
        public string? Comment { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"firewall security group '{Group}'", "Create"))
                return;

            var session = GetSession();
            var service = new FirewallService();

            WriteVerbose($"Creating firewall security group '{Group}'...");
            service.CreateGroup(session, Group, Comment);
        }
    }
}
