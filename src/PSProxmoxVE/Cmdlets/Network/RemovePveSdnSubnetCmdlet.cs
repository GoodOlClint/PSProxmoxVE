using System.Management.Automation;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Removes an SDN subnet from a VNet in Proxmox VE.</para>
    /// <para type="description">
    /// Deletes the specified Software-Defined Networking subnet from the given VNet.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveSdnSubnet", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public class RemovePveSdnSubnetCmdlet : PveCmdletBase
    {
        /// <summary>The SDN VNet containing the subnet.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The SDN VNet name.")]
        public string Vnet { get; set; } = string.Empty;

        /// <summary>The subnet CIDR to remove (e.g. "10.0.0.0/24").</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The subnet CIDR to remove.")]
        public string Subnet { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Subnet {Subnet} on VNet {Vnet}", "Remove PVE SDN Subnet"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            WriteVerbose($"Removing SDN subnet '{Subnet}' from VNet '{Vnet}'...");
            client.DeleteAsync(
                $"cluster/sdn/vnets/{System.Uri.EscapeDataString(Vnet)}/subnets/{System.Uri.EscapeDataString(Subnet)}")
                .GetAwaiter().GetResult();
        }
    }
}
