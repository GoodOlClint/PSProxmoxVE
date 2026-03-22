using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Updates a Proxmox VE role's privileges.</para>
    /// <para type="description">
    /// Modifies the privilege set for an existing custom role.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveRole", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveRoleCmdlet : PveCmdletBase
    {
        /// <summary>The role identifier to update.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The role identifier.")]
        public string RoleId { get; set; } = string.Empty;

        /// <summary>Comma-separated list of privileges to assign to the role.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "Comma-separated list of privileges (e.g. 'VM.Audit,VM.Console').")]
        public string Privileges { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"role '{RoleId}'", "Set"))
                return;

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Updating role '{RoleId}'...");
            service.UpdateRole(session, RoleId, Privileges);
        }
    }
}
