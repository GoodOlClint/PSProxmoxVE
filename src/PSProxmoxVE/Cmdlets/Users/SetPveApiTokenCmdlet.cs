using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Updates a Proxmox VE API token configuration.</para>
    /// <para type="description">
    /// Modifies properties of an existing API token such as its comment,
    /// expiration, or privilege separation setting.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveApiToken", SupportsShouldProcess = true)]
    public sealed class SetPveApiTokenCmdlet : PveCmdletBase
    {
        /// <summary>The user ID that owns the token (e.g. 'user@realm').</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The user ID (e.g. 'user@realm').")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>The token identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The token identifier.")]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>Optional comment for the token.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comment describing the token.")]
        public string? Comment { get; set; }

        /// <summary>Token expiration as a Unix epoch timestamp. Set to 0 for no expiration.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Expiration as Unix epoch (0 = no expiration).")]
        public long? Expire { get; set; }

        /// <summary>Whether the token uses privilege separation.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Whether the token uses privilege separation.")]
        public SwitchParameter PrivilegeSeparation { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"API token '{UserId}!{TokenId}'", "Set"))
                return;

            var session = GetSession();
            var service = new UserService();

            var config = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(Comment)) config["comment"] = Comment!;
            if (Expire.HasValue) config["expire"] = Expire.Value.ToString();
            if (MyInvocation.BoundParameters.ContainsKey("PrivilegeSeparation"))
                config["privsep"] = PrivilegeSeparation.IsPresent ? "1" : "0";

            WriteVerbose($"Updating API token '{UserId}!{TokenId}'...");
            service.UpdateApiToken(session, UserId, TokenId, config);
        }
    }
}
