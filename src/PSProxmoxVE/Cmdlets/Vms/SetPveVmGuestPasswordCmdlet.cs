using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Sets a user password inside the guest via the QEMU guest agent.</para>
    /// <para type="description">
    /// Changes a user's password inside the guest operating system using the QEMU guest agent.
    /// The guest agent must be installed and running inside the VM.
    /// Use -Crypted if the password is already in a hashed/crypted format.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveVmGuestPassword", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveVmGuestPasswordCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The username whose password should be changed.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The username whose password to set.")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The new password for the user.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The new password.")]
        public SecureString Password { get; set; } = new SecureString();

        /// <summary>
        /// <para type="description">When specified, indicates the password is already in crypted/hashed format.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Indicates the password is already crypted/hashed.")]
        public SwitchParameter Crypted { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"User '{Username}' on VM {VmId} (node '{Node}')", "Set-PveVmGuestPassword"))
                return;

            var session = GetSession();

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

            WriteVerbose($"Setting password for user '{Username}' on VM {VmId} via guest agent...");
            var service = new VmService();
            service.SetGuestPassword(session, Node, VmId, Username, plainPassword, Crypted.IsPresent);
        }
    }
}
