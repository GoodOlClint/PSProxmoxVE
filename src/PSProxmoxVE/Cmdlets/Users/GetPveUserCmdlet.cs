using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Users;

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
    public class GetPveUserCmdlet : PveCmdletBase
    {
        /// <summary>
        /// Filter results to a specific user ID or pattern (e.g., "admin@pam", "*@pve").
        /// Supports wildcard (*) matching.
        /// </summary>
        [Parameter(Mandatory = false, Position = 0)]
        public string? UserId { get; set; }

        /// <summary>When specified, returns only enabled users.</summary>
        [Parameter(Mandatory = false)]
        [Alias("EnabledOnly")]
        public SwitchParameter Enabled { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            using var client = new PveHttpClient(session);

            var json = client.GetAsync("access/users").GetAwaiter().GetResult();
            var root = JObject.Parse(json);
            var data = root["data"] as JArray ?? new JArray();

            foreach (var item in data)
            {
                var user = item.ToObject<PveUser>()!;
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

            if (UserId.Contains("*"))
            {
                var pattern = UserId.Replace("*", "");
                return user.UserId.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return string.Equals(user.UserId, UserId, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
