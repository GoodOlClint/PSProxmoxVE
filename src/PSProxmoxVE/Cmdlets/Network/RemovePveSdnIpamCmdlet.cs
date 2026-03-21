using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Removes an SDN IPAM plugin from Proxmox VE.</para>
    /// <para type="description">
    /// Deletes the specified Software-Defined Networking IPAM plugin.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveSdnIpam", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class RemovePveSdnIpamCmdlet : PveCmdletBase
    {
        /// <summary>The IPAM plugin identifier to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The IPAM plugin identifier to remove.")]
        public string Ipam { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"SDN IPAM '{Ipam}'", "Remove PVE SDN IPAM"))
                return;

            var session = GetSession();
            var service = new NetworkService();

            WriteVerbose($"Removing SDN IPAM plugin '{Ipam}'...");
            service.RemoveSdnIpam(session, Ipam);
        }
    }
}
