using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Creates a new Proxmox VE role.</para>
    /// <para type="description">
    /// Adds a new role to the Proxmox VE access management system with the specified privileges.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveRole", SupportsShouldProcess = true)]
    [OutputType(typeof(PveRole))]
    public sealed class NewPveRoleCmdlet : PveCmdletBase
    {
        /// <summary>The role identifier/name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The role identifier.")]
        public string RoleId { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated list of privileges to grant this role
        /// (e.g., "VM.Allocate,VM.Config.CPU,VM.Config.Memory").
        /// </summary>
        [Parameter(Mandatory = false, Position = 1, HelpMessage = "Comma-separated list of privileges.")]
        public string? Privileges { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(RoleId, "Create PVE Role"))
                return;

            var session = GetSession();

            WriteVerbose($"Creating role '{RoleId}'...");
            var service = new UserService();
            service.CreateRole(session, RoleId, Privileges);

            WriteObject(new PveRole { RoleId = RoleId, Privileges = Privileges });
        }
    }
}
