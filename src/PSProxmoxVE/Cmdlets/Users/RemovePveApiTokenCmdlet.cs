using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Removes an API token from a Proxmox VE user.</para>
    /// <para type="description">
    /// Permanently deletes the specified API token. Any automation using this token will
    /// immediately lose access. This operation cannot be undone.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveApiToken", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    public class RemovePveApiTokenCmdlet : PveCmdletBase
    {
        /// <summary>
        /// The user ID that owns the token, in "username@realm" format (e.g., "admin@pam").
        /// Accepts pipeline input from Get-PveApiToken (PveApiToken.UserId).
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The token identifier to remove (e.g., "automation").
        /// Accepts pipeline input from Get-PveApiToken (PveApiToken.TokenId).
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public string TokenId { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            var session = GetSession();

            if (!ShouldProcess($"{UserId}!{TokenId}", "Remove PVE API Token"))
                return;

            var service = new UserService();
            service.RemoveApiToken(session, UserId, TokenId);
        }
    }
}
