using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Users;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Creates a new Proxmox VE role.</para>
    /// <para type="description">
    /// Adds a new role to the Proxmox VE access management system with the specified privileges.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveRole", SupportsShouldProcess = true)]
    [OutputType(typeof(PveRole))]
    public class NewPveRoleCmdlet : PveCmdletBase
    {
        /// <summary>The role identifier/name.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string RoleId { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated list of privileges to grant this role
        /// (e.g., "VM.Allocate,VM.Config.CPU,VM.Config.Memory").
        /// </summary>
        [Parameter(Mandatory = false, Position = 1)]
        public string? Privileges { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(RoleId, "Create PVE Role"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            var data = new Dictionary<string, string>
            {
                ["roleid"] = RoleId
            };
            if (!string.IsNullOrEmpty(Privileges)) data["privs"] = Privileges;

            client.PostAsync("access/roles", data).GetAwaiter().GetResult();

            WriteObject(new PveRole { RoleId = RoleId, Privileges = Privileges });
        }
    }
}
