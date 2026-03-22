using System.Management.Automation;
using PSProxmoxVE.Core.Models.Network;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Lists SDN controllers in Proxmox VE.</para>
    /// <para type="description">
    /// Returns Software-Defined Networking controller definitions.
    /// Optionally filters by controller identifier.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveSdnController")]
    [OutputType(typeof(PveSdnController))]
    public sealed class GetPveSdnControllerCmdlet : PveCmdletBase
    {
        /// <summary>Optional controller identifier filter.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "Filter by controller identifier.")]
        public string? Controller { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "SDN IPAM/DNS/Controller", 6, 2, 8, 1);
            var service = new NetworkService();

            WriteVerbose("Getting SDN controllers...");
            var controllers = service.GetSdnControllers(session);

            foreach (var item in controllers)
            {
                if (!string.IsNullOrEmpty(Controller) &&
                    !string.Equals(item.Controller, Controller, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteObject(item);
            }
        }
    }
}
