using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Creates a new Proxmox VE authentication domain/realm.</para>
    /// <para type="description">
    /// Adds a new authentication domain (realm) to the Proxmox VE access management system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveDomain", SupportsShouldProcess = true)]
    [OutputType(typeof(PveDomain))]
    public class NewPveDomainCmdlet : PveCmdletBase
    {
        /// <summary>The realm identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The realm identifier.")]
        [ValidateNotNullOrEmpty]
        public string Realm { get; set; } = string.Empty;

        /// <summary>The domain type.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The domain type (pam, pve, ad, ldap, openid).")]
        [ValidateSet("pam", "pve", "ad", "ldap", "openid", IgnoreCase = true)]
        public string Type { get; set; } = string.Empty;

        /// <summary>Optional comment/description for the domain.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comment or description for the domain.")]
        public string? Comment { get; set; }

        /// <summary>Set this realm as the default authentication domain.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Set as the default authentication domain.")]
        public SwitchParameter Default { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Realm, "Create PVE Domain"))
                return;

            var config = new Dictionary<string, string>
            {
                ["realm"] = Realm,
                ["type"] = Type
            };
            if (!string.IsNullOrEmpty(Comment)) config["comment"] = Comment!;
            if (Default.IsPresent) config["default"] = "1";

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Creating domain '{Realm}' of type '{Type}'...");
            service.CreateDomain(session, config);

            WriteObject(new PveDomain
            {
                Realm = Realm,
                Type = Type,
                Comment = Comment,
                Default = Default.IsPresent ? 1 : 0
            });
        }
    }
}
