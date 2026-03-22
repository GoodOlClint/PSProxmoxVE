using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Creates a new Proxmox VE group.</para>
    /// <para type="description">
    /// Adds a new group to the Proxmox VE access management system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(PveGroup))]
    public sealed class NewPveGroupCmdlet : PveCmdletBase
    {
        /// <summary>The group identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The group identifier.")]
        [ValidateNotNullOrEmpty]
        public string GroupId { get; set; } = string.Empty;

        /// <summary>Optional comment/description for the group.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comment or description for the group.")]
        public string? Comment { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(GroupId, "Create PVE Group"))
                return;

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Creating group '{GroupId}'...");
            service.CreateGroup(session, GroupId, Comment);

            WriteObject(new PveGroup
            {
                GroupId = GroupId,
                Comment = Comment
            });
        }
    }
}
