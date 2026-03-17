using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Removes a Proxmox VE user account.</para>
    /// <para type="description">
    /// Deletes the specified user from the Proxmox VE access management system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveUser", SupportsShouldProcess = true)]
    public class RemovePveUserCmdlet : PveCmdletBase
    {
        /// <summary>The user identifier to remove (e.g., "jdoe@pve").</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string UserId { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(UserId, "Remove PVE User"))
                return;

            var session = GetSession();
            var service = new UserService();
            service.RemoveUser(session, UserId);
        }
    }
}
