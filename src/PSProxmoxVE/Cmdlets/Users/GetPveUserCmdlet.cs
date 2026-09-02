using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Lists Proxmox VE user accounts.</para>
    /// <para type="description">
    /// Returns user accounts from the Proxmox VE access management system.
    /// Optionally filter by user ID (supports wildcard matching).
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveUser")]
    [OutputType(typeof(PveUser))]
    public sealed class GetPveUserCmdlet : PveCmdletBase
    {
        /// <summary>
        /// Filter results to a specific user ID or pattern (e.g., "admin@pam", "*@pve").
        /// Supports wildcard (*) matching.
        /// </summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "The user ID in user@realm format.")]
        public string? UserId { get; set; }

        /// <summary>When specified, returns only enabled users.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Return only enabled users.")]
        [Alias("EnabledOnly")]
        public SwitchParameter Enabled { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose("Getting users...");
            var service = new UserService();
            var users = service.GetUsers(session);

            foreach (var user in users)
            {
                if (MatchesFilters(user))
                    WriteObject(user);
            }
        }

        private bool MatchesFilters(PveUser user)
        {
            if (Enabled.IsPresent && user.Enabled.GetValueOrDefault() != 1)
                return false;

            if (string.IsNullOrEmpty(UserId))
                return true;

            if (UserId!.Contains("*"))
            {
                var pattern = UserId.Replace("*", "");
                return user.UserId.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return string.Equals(user.UserId, UserId, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
