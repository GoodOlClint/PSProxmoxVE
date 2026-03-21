using System.Management.Automation;
using PSProxmoxVE.Core.Models.Network;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Lists SDN DNS plugins in Proxmox VE.</para>
    /// <para type="description">
    /// Returns Software-Defined Networking DNS plugin definitions.
    /// Optionally filters by DNS identifier.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveSdnDns")]
    [OutputType(typeof(PveSdnDns))]
    public class GetPveSdnDnsCmdlet : PveCmdletBase
    {
        /// <summary>Optional DNS plugin identifier filter.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "Filter by DNS plugin identifier.")]
        public string? Dns { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new NetworkService();

            WriteVerbose("Getting SDN DNS plugins...");
            var plugins = service.GetSdnDnsPlugins(session);

            foreach (var item in plugins)
            {
                if (!string.IsNullOrEmpty(Dns) &&
                    !string.Equals(item.Dns, Dns, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteObject(item);
            }
        }
    }
}
