using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Users;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Lists Proxmox VE roles.</para>
    /// <para type="description">
    /// Returns role definitions from the Proxmox VE access management system.
    /// Roles are named sets of privileges that can be assigned via ACLs.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveRole")]
    [OutputType(typeof(PveRole))]
    public class GetPveRoleCmdlet : PveCmdletBase
    {
        /// <summary>Optional role identifier to retrieve a specific role.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "The role identifier.")]
        public string? RoleId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            using var client = new PveHttpClient(session);

            WriteVerbose("Getting roles...");
            var json = client.GetAsync("access/roles").GetAwaiter().GetResult();
            var root = JObject.Parse(json);
            var data = root["data"] as JArray ?? new JArray();

            foreach (var item in data)
            {
                var role = item.ToObject<PveRole>()!;
                if (!string.IsNullOrEmpty(RoleId) &&
                    !string.Equals(role.RoleId, RoleId, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                WriteObject(role);
            }
        }
    }
}
