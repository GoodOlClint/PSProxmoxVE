using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Removes a Proxmox VE role.</para>
    /// <para type="description">
    /// Deletes the specified custom role from the Proxmox VE access management system.
    /// Built-in roles cannot be removed.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveRole", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class RemovePveRoleCmdlet : PveCmdletBase
    {
        /// <summary>The role identifier to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The role identifier.")]
        public string RoleId { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(RoleId, "Remove PVE Role"))
                return;

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Removing role '{RoleId}'...");
            service.RemoveRole(session, RoleId);
        }
    }
}
