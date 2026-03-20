using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Creates a new API token for a Proxmox VE user.</para>
    /// <para type="description">
    /// Generates a new API token and returns a PveApiToken object containing both the
    /// FullTokenId (e.g., "admin@pam!automation") and the token secret in the Value
    /// property. The secret is shown only once — save it immediately. Use the FullTokenId
    /// and Value together as the API token credential for Connect-PveServer.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveApiToken", SupportsShouldProcess = true)]
    [OutputType(typeof(PveApiToken))]
    public class NewPveApiTokenCmdlet : PveCmdletBase
    {
        /// <summary>
        /// The user ID to create the token for, in "username@realm" format (e.g., "admin@pam").
        /// Accepts pipeline input from Get-PveUser (PveUser.UserId).
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The user ID in user@realm format.")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>The token identifier (alphanumeric, hyphens allowed; e.g., "automation").</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The API token identifier.")]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>Optional description for this token.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Description for this token.")]
        public string? Comment { get; set; }

        /// <summary>
        /// Token expiry as a Unix timestamp. Use 0 or omit for no expiry.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Token expiry as a Unix timestamp.")]
        public long? Expire { get; set; }

        /// <summary>
        /// When specified, privilege separation is enabled: the token's effective permissions
        /// are the intersection of the user's ACLs and any explicit ACLs granted to the token.
        /// When omitted, the token inherits the full permissions of its user.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Enable token privilege separation.")]
        public SwitchParameter PrivilegeSeparation { get; set; }

        protected override void ProcessRecord()
        {
            var fullTokenId = $"{UserId}!{TokenId}";
            if (!ShouldProcess(fullTokenId, "Create PVE API Token"))
                return;

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Creating API token '{TokenId}' for user '{UserId}'...");
            var token = service.CreateApiToken(
                session,
                UserId,
                TokenId,
                comment:             Comment,
                expire:              Expire,
                privilegeSeparation: PrivilegeSeparation.IsPresent ? true : (bool?)null);

            WriteObject(token);
        }
    }
}
