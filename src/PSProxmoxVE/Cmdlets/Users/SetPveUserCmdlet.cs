using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Updates a Proxmox VE user account.</para>
    /// <para type="description">
    /// Modifies properties of an existing Proxmox VE user account. Only specified parameters
    /// are updated; omitted parameters retain their current values.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveUser", SupportsShouldProcess = true)]
    public class SetPveUserCmdlet : PveCmdletBase
    {
        /// <summary>The user identifier to update (e.g., "jdoe@pve").</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The user ID in user@realm format.")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>New password for the user. Accepts a SecureString.</summary>
        [Parameter(Mandatory = false, HelpMessage = "New password for the user.")]
        public System.Security.SecureString? Password { get; set; }

        /// <summary>Updated first name.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Updated first name.")]
        public string? FirstName { get; set; }

        /// <summary>Updated last name.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Updated last name.")]
        public string? LastName { get; set; }

        /// <summary>Updated email address.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Updated email address.")]
        public string? Email { get; set; }

        /// <summary>Updated group membership (comma-separated group names).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Updated group membership (comma-separated).")]
        public string? Groups { get; set; }

        /// <summary>Updated comment/notes.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Updated comment or notes.")]
        public string? Comment { get; set; }

        /// <summary>Account expiry as a Unix timestamp. Use 0 to remove expiry.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Account expiry as a Unix timestamp.")]
        public long? Expire { get; set; }

        /// <summary>Enable or disable the user account.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Enable or disable the user account.")]
        public bool? Enable { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(UserId, "Set PVE User"))
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
            if (Enable.HasValue)                  config["enable"]    = Enable.Value ? "1" : "0";

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Updating user '{UserId}'...");
            service.SetUser(session, UserId, config);
        }
    }
}
