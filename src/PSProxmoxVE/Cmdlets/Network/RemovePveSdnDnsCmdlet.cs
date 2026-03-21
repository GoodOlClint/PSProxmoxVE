using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Removes an SDN DNS plugin from Proxmox VE.</para>
    /// <para type="description">
    /// Deletes the specified Software-Defined Networking DNS plugin.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveSdnDns", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class RemovePveSdnDnsCmdlet : PveCmdletBase
    {
        /// <summary>The DNS plugin identifier to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The DNS plugin identifier to remove.")]
        public string Dns { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"SDN DNS '{Dns}'", "Remove PVE SDN DNS"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN IPAM/DNS/Controller", 6, 2, 8, 1);
            var service = new NetworkService();

            WriteVerbose($"Removing SDN DNS plugin '{Dns}'...");
            service.RemoveSdnDnsPlugin(session, Dns);
        }
    }
}
