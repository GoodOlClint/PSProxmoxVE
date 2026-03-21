using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Updates a Proxmox VE authentication domain/realm.</para>
    /// <para type="description">
    /// Modifies properties of an existing authentication domain (realm) in the
    /// Proxmox VE access management system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveDomain", SupportsShouldProcess = true)]
    public class SetPveDomainCmdlet : PveCmdletBase
    {
        /// <summary>The realm identifier to update.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The realm identifier.")]
        [ValidateNotNullOrEmpty]
        public string Realm { get; set; } = string.Empty;

        /// <summary>Updated comment/description for the domain.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Updated comment or description for the domain.")]
        public string? Comment { get; set; }

        /// <summary>Set this realm as the default authentication domain.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Set as the default authentication domain.")]
        public SwitchParameter Default { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Realm, "Set PVE Domain"))
                return;

            var config = new Dictionary<string, string>();
            if (Comment != null) config["comment"] = Comment;
            if (Default.IsPresent) config["default"] = "1";

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Updating domain '{Realm}'...");
            service.UpdateDomain(session, Realm, config);
        }
    }
}
