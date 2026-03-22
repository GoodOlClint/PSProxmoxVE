using System.Management.Automation;
using System.Runtime.InteropServices;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Changes a Proxmox VE user's password.</para>
    /// <para type="description">
    /// Updates the password for the specified user via the /access/password endpoint.
    /// The password is accepted as a SecureString for safe handling in PowerShell.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PvePassword", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPvePasswordCmdlet : PveCmdletBase
    {
        /// <summary>The user identifier in "user@realm" format.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The user ID in user@realm format.")]
        [ValidateNotNullOrEmpty]
        public string UserId { get; set; } = string.Empty;

        /// <summary>The new password for the user.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The new password for the user.")]
        public System.Security.SecureString Password { get; set; } = new System.Security.SecureString();

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(UserId, "Set PVE Password"))
                return;

            var ptr = Marshal.SecureStringToGlobalAllocUnicode(Password);
            string plainPassword;
            try
            {
                plainPassword = Marshal.PtrToStringUni(ptr) ?? string.Empty;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Changing password for user '{UserId}'...");
            service.ChangePassword(session, UserId, plainPassword);
        }
    }
}
