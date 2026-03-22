using System.Linq;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Users;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Lists Proxmox VE authentication domains/realms.</para>
    /// <para type="description">
    /// Returns all authentication domains (realms) from the Proxmox VE access management
    /// system. Optionally filter by a specific realm name.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveDomain")]
    [OutputType(typeof(PveDomain))]
    public sealed class GetPveDomainCmdlet : PveCmdletBase
    {
        /// <summary>Optional realm name to filter by.</summary>
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "Optional realm name to filter results.")]
        public string? Realm { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new UserService();

            WriteVerbose("Getting authentication domains...");
            var domains = service.GetDomains(session);

            if (!string.IsNullOrEmpty(Realm))
            {
                domains = domains.Where(d => d.Realm == Realm).ToArray();
            }

            foreach (var domain in domains)
            {
                WriteObject(domain);
            }
        }
    }
}
