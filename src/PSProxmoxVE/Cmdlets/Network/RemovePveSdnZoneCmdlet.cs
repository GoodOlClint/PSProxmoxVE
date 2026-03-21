using System.Management.Automation;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Removes an SDN zone from Proxmox VE.</para>
    /// <para type="description">
    /// Deletes the specified Software-Defined Networking zone from the cluster configuration.
    /// All VNets within this zone must be removed first.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveSdnZone", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class RemovePveSdnZoneCmdlet : PveCmdletBase
    {
        /// <summary>The zone identifier to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The SDN zone name.")]
        public string Zone { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Zone, "Remove PVE SDN Zone"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);
            using var client = new PveHttpClient(session);

            WriteVerbose($"Removing SDN zone '{Zone}'...");
            client.DeleteAsync($"cluster/sdn/zones/{Zone}").GetAwaiter().GetResult();
        }
    }
}
