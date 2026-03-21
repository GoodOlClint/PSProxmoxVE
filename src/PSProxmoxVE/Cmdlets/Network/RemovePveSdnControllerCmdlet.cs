using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Removes an SDN controller from Proxmox VE.</para>
    /// <para type="description">
    /// Deletes the specified Software-Defined Networking controller.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveSdnController", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class RemovePveSdnControllerCmdlet : PveCmdletBase
    {
        /// <summary>The controller identifier to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The controller identifier to remove.")]
        public string Controller { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"SDN Controller '{Controller}'", "Remove PVE SDN Controller"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN IPAM/DNS/Controller", 6, 2, 8, 1);
            var service = new NetworkService();

            WriteVerbose($"Removing SDN controller '{Controller}'...");
            service.RemoveSdnController(session, Controller);
        }
    }
}
