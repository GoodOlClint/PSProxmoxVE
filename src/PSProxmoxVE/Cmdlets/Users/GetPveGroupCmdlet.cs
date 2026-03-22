using System.Linq;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Lists Proxmox VE groups.</para>
    /// <para type="description">
    /// Returns all groups from the Proxmox VE access management system. Optionally
    /// filter by a specific group ID.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveGroup")]
    [OutputType(typeof(PveGroup))]
    public sealed class GetPveGroupCmdlet : PveCmdletBase
    {
        /// <summary>Optional group ID to filter by.</summary>
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "Optional group ID to filter results.")]
        public string? GroupId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new UserService();

            WriteVerbose("Getting groups...");
            var groups = service.GetGroups(session);

            if (!string.IsNullOrEmpty(GroupId))
            {
                groups = groups.Where(g => g.GroupId == GroupId).ToArray();
            }

            foreach (var group in groups)
            {
                WriteObject(group);
            }
        }
    }
}
