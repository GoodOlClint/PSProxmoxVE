using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Lists ACL entries (permissions) in Proxmox VE.</para>
    /// <para type="description">
    /// Returns Access Control List entries from the Proxmox VE access management system.
    /// Optionally filter by path or user/group ID.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PvePermission")]
    [OutputType(typeof(PvePermission))]
    public class GetPvePermissionCmdlet : PveCmdletBase
    {
        /// <summary>Filter results to a specific resource path (e.g., "/", "/vms/100").</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "Filter by resource path (e.g. /, /vms/100).")]
        public string? Path { get; set; }

        /// <summary>Filter results to a specific user or group ID.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The user ID in user@realm format.")]
        public string? UserId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose("Getting permissions...");
            var service = new UserService();

            var permissions = service.GetPermissions(session);

            foreach (var perm in permissions)
            {
                if (!string.IsNullOrEmpty(Path) &&
                    !string.Equals(perm.Path, Path, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(UserId) &&
                    !string.Equals(perm.UserId, UserId, StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteObject(perm);
            }
        }
    }
}
