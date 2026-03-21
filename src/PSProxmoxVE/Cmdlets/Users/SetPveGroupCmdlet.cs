using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Updates a Proxmox VE group.</para>
    /// <para type="description">
    /// Modifies properties of an existing group in the Proxmox VE access management system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveGroup", SupportsShouldProcess = true)]
    public class SetPveGroupCmdlet : PveCmdletBase
    {
        /// <summary>The group identifier to update.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The group identifier.")]
        [ValidateNotNullOrEmpty]
        public string GroupId { get; set; } = string.Empty;

        /// <summary>Updated comment/description for the group.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Updated comment or description for the group.")]
        public string? Comment { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(GroupId, "Set PVE Group"))
                return;

            var config = new Dictionary<string, string>();
            if (Comment != null) config["comment"] = Comment;

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Updating group '{GroupId}'...");
            service.UpdateGroup(session, GroupId, config);
        }
    }
}
