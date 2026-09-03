using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Lists Proxmox VE roles.</para>
    /// <para type="description">
    /// Returns role definitions from the Proxmox VE access management system.
    /// Roles are named sets of privileges that can be assigned via ACLs.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveRole")]
    [OutputType(typeof(PveRole))]
    public sealed class GetPveRoleCmdlet : PveCmdletBase
    {
        /// <summary>Optional role identifier to retrieve a specific role.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "The role identifier.")]
        public string? RoleId { get; set; }

        protected override void ProcessPveRecord()
        {
            var session = GetSession();

            WriteVerbose("Getting roles...");
            var service = new UserService();
            var roles = service.GetRoles(session);

            foreach (var role in roles)
            {
                if (!string.IsNullOrEmpty(RoleId) &&
                    !string.Equals(role.RoleId, RoleId, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                WriteObject(role);
            }
        }
    }
}
