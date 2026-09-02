using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Sets or updates ACL entries (permissions) in Proxmox VE.</para>
    /// <para type="description">
    /// Adds or modifies Access Control List entries in the Proxmox VE access management system.
    /// To remove a permission, use the -Delete switch.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PvePermission", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPvePermissionCmdlet : PveCmdletBase
    {
        /// <summary>The resource path this ACL applies to (e.g., "/", "/vms/100").</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The resource path (e.g. /, /vms/100).")]
        public string Path { get; set; } = string.Empty;

        /// <summary>The user or group identifier (e.g., "jdoe@pve" or "admins").</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The user or group identifier.")]
        public string UgId { get; set; } = string.Empty;

        /// <summary>The role to assign (e.g., "Administrator", "PVEVMUser").</summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "The role to assign (e.g. Administrator).")]
        public string Role { get; set; } = string.Empty;

        /// <summary>The ACL entry type: "user", "token", or "group". When set to "user", API tokens
        /// (UgId containing "!") are automatically detected and sent as the "tokens" parameter.</summary>
        [Parameter(Mandatory = false, HelpMessage = "ACL entry type: user, token, or group.")]
        [ValidateSet("user", "token", "group", IgnoreCase = true)]
        public string Type { get; set; } = "user";

        /// <summary>Whether to propagate this ACL to child paths.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Propagate this ACL to child paths.")]
        public SwitchParameter Propagate { get; set; }

        /// <summary>When specified, removes the ACL entry instead of adding it.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Remove the ACL entry instead of adding it.")]
        public SwitchParameter Delete { get; set; }

        protected override void ProcessRecord()
        {
            var action = Delete.IsPresent ? "Remove" : "Set";
            if (!ShouldProcess($"{Type} '{UgId}' at '{Path}'", $"{action} PVE Permission ({Role})"))
                return;

            var session = GetSession();

            WriteVerbose($"Setting permission for '{UgId}' at '{Path}'...");
            string? users = null, groups = null, tokens = null;
            if (string.Equals(Type, "group", System.StringComparison.OrdinalIgnoreCase))
                groups = UgId;
            else if (string.Equals(Type, "token", System.StringComparison.OrdinalIgnoreCase) || UgId.Contains("!"))
                tokens = UgId;
            else
                users = UgId;

            var service = new UserService();
            service.SetPermission(
                session,
                Path,
                Role,
                users: users,
                groups: groups,
                tokens: tokens,
                propagate: Propagate.IsPresent ? true : (bool?)null,
                delete: Delete.IsPresent ? true : (bool?)null);
        }
    }
}
