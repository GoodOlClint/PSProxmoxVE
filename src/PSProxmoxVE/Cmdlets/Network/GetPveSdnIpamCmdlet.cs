using System.Management.Automation;
using PSProxmoxVE.Core.Models.Network;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Lists SDN IPAM plugins in Proxmox VE.</para>
    /// <para type="description">
    /// Returns Software-Defined Networking IPAM plugin definitions.
    /// Optionally filters by IPAM identifier.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveSdnIpam")]
    [OutputType(typeof(PveSdnIpam))]
    public class GetPveSdnIpamCmdlet : PveCmdletBase
    {
        /// <summary>Optional IPAM identifier filter.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "Filter by IPAM plugin identifier.")]
        public string? Ipam { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new NetworkService();

            WriteVerbose("Getting SDN IPAM plugins...");
            var ipams = service.GetSdnIpams(session);

            foreach (var item in ipams)
            {
                if (!string.IsNullOrEmpty(Ipam) &&
                    !string.Equals(item.Ipam, Ipam, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteObject(item);
            }
        }
    }
}
