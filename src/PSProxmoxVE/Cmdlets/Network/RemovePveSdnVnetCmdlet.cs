using System.Management.Automation;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Removes an SDN VNet from Proxmox VE.</para>
    /// <para type="description">
    /// Deletes the specified Software-Defined Networking VNet from the cluster SDN configuration.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveSdnVnet", SupportsShouldProcess = true)]
    public class RemovePveSdnVnetCmdlet : PveCmdletBase
    {
        /// <summary>The VNet identifier to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string Vnet { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Vnet, "Remove PVE SDN VNet"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            client.DeleteAsync($"/cluster/sdn/vnets/{Vnet}").GetAwaiter().GetResult();
        }
    }
}
