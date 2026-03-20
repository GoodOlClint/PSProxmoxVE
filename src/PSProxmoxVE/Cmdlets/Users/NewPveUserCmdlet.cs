using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Creates a new Proxmox VE user account.</para>
    /// <para type="description">
    /// Adds a new user to the Proxmox VE access management system. The user ID must
    /// include the realm (e.g., "newuser@pve" or "newuser@pam").
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveUser", SupportsShouldProcess = true)]
    [OutputType(typeof(PveUser))]
    public class NewPveUserCmdlet : PveCmdletBase
    {
        /// <summary>The user identifier in "user@realm" format (e.g., "jdoe@pve").</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The user ID in user@realm format.")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>The user's password (for pve/pam realms). Accepts a SecureString.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Password for the user.")]
        public System.Security.SecureString? Password { get; set; }

        /// <summary>The user's first name.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The user's first name.")]
        public string? FirstName { get; set; }

        /// <summary>The user's last name.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The user's last name.")]
        public string? LastName { get; set; }

        /// <summary>The user's email address.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The user's email address.")]
        public string? Email { get; set; }

        /// <summary>Comma-separated list of groups to add this user to.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of groups.")]
        public string? Groups { get; set; }

        /// <summary>Optional comment/notes for this user.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comment or notes for this user.")]
        public string? Comment { get; set; }

        /// <summary>Account expiry as a Unix timestamp. Use 0 for no expiry.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Account expiry as a Unix timestamp.")]
        public long? Expire { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(UserId, "Create PVE User"))
                return;

            var config = new Dictionary<string, object>();

            if (Password != null)
            {
                var ptr = Marshal.SecureStringToGlobalAllocUnicode(Password);
                try { config["password"] = Marshal.PtrToStringUni(ptr) ?? string.Empty; }
                finally { Marshal.ZeroFreeGlobalAllocUnicode(ptr); }
            }

            if (!string.IsNullOrEmpty(FirstName)) config["firstname"] = FirstName!;
            if (!string.IsNullOrEmpty(LastName))  config["lastname"]  = LastName!;
            if (!string.IsNullOrEmpty(Email))     config["email"]     = Email!;
            if (!string.IsNullOrEmpty(Groups))    config["groups"]    = Groups!;
            if (!string.IsNullOrEmpty(Comment))   config["comment"]   = Comment!;
            if (Expire.HasValue)                  config["expire"]    = Expire.Value;

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Creating user '{UserId}'...");
            service.CreateUser(session, UserId, config);

            WriteObject(new PveUser
            {
                UserId    = UserId,
                FirstName = FirstName,
                LastName  = LastName,
                Email     = Email,
                Groups    = Groups,
                Comment   = Comment,
                Expire    = Expire,
                Enabled   = 1
            });
        }
    }
}
