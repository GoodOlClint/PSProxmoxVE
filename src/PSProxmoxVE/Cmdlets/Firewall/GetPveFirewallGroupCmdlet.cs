using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Firewall;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Firewall
{
    [Cmdlet(VerbsCommon.Get, "PveFirewallGroup")]
    [OutputType(typeof(PveFirewallGroup), typeof(PveFirewallRule))]
    public sealed class GetPveFirewallGroupCmdlet : PveCmdletBase
    {
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "The security group name. If specified, returns rules within the group.")]
        public string? Group { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new FirewallService();

            if (!string.IsNullOrEmpty(Group))
            {
                WriteVerbose($"Getting firewall rules for security group '{Group}'...");
                var rules = service.GetGroupRules(session, Group!);
                WriteObject(rules, true);
            }
            else
            {
                WriteVerbose("Getting firewall security groups...");
                var groups = service.GetGroups(session);
                WriteObject(groups, true);
            }
        }
    }
}
