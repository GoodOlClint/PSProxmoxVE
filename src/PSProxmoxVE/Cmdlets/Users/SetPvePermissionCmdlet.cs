using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Client;

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

        /// <summary>The ACL entry type: "user" or "group".</summary>
        [Parameter(Mandatory = false, HelpMessage = "ACL entry type: user or group.")]
        [ValidateSet("user", "group", IgnoreCase = true)]
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
            using var client = new PveHttpClient(session);

            WriteVerbose($"Setting permission for '{UgId}' at '{Path}'...");
            var data = new Dictionary<string, string>
            {
                ["path"]  = Path,
                ["roles"] = Role
            };

            if (string.Equals(Type, "group", System.StringComparison.OrdinalIgnoreCase))
                data["groups"] = UgId;
            else
                data["users"] = UgId;

            if (Propagate.IsPresent) data["propagate"] = "1";
            if (Delete.IsPresent)    data["delete"]    = "1";

            client.PutAsync("access/acl", data).GetAwaiter().GetResult();
        }
    }
}
