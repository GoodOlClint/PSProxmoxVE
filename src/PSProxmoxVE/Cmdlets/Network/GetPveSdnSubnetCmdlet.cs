using System.Management.Automation;
using PSProxmoxVE.Core.Models.Network;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Lists SDN subnets for a VNet in Proxmox VE.</para>
    /// <para type="description">
    /// Returns Software-Defined Networking subnet definitions for the specified VNet.
    /// Requires Proxmox VE 8.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveSdnSubnet")]
    [OutputType(typeof(PveSdnSubnet))]
    public sealed class GetPveSdnSubnetCmdlet : PveCmdletBase
    {
        /// <summary>The SDN VNet to list subnets for.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The SDN VNet name.")]
        public string Vnet { get; set; } = string.Empty;

        /// <summary>Optional subnet CIDR filter.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by subnet CIDR (e.g. 10.0.0.0/24).")]
        public string? Subnet { get; set; }

        protected override void ProcessPveRecord()
        {
            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);

            WriteVerbose($"Getting SDN subnets for VNet '{Vnet}'...");
            var service = new NetworkService();
            var subnets = service.GetSdnSubnets(session, Vnet);

            foreach (var subnet in subnets)
            {
                if (!string.IsNullOrEmpty(Subnet) &&
                    !string.Equals(subnet.Subnet, Subnet, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                WriteObject(subnet);
            }
        }
    }
}
