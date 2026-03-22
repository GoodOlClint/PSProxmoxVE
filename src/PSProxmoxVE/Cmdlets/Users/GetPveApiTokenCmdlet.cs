using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Lists API tokens for a Proxmox VE user.</para>
    /// <para type="description">
    /// Returns the API tokens associated with the specified user. When -TokenId is omitted
    /// all tokens for the user are returned. Note: the token secret (Value) is never
    /// returned by the GET endpoints — it is only available immediately after creation
    /// via New-PveApiToken.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveApiToken")]
    [OutputType(typeof(PveApiToken))]
    public sealed class GetPveApiTokenCmdlet : PveCmdletBase
    {
        /// <summary>
        /// The user ID whose tokens to list, in "username@realm" format (e.g., "admin@pam").
        /// Accepts pipeline input from Get-PveUser (PveUser.UserId).
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The user ID in user@realm format.")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>Filter to a specific token identifier (e.g., "automation").</summary>
        [Parameter(Mandatory = false, Position = 1, HelpMessage = "The API token identifier.")]
        public string? TokenId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose($"Getting API tokens for user '{UserId}'...");
            var service = new UserService();
            var tokens  = service.GetApiTokens(session, UserId);

            foreach (var token in tokens)
            {
                if (!string.IsNullOrEmpty(TokenId) &&
                    !string.Equals(token.TokenId, TokenId, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteObject(token);
            }
        }
    }
}
