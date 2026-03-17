using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Creates a new SDN VNet in Proxmox VE.</para>
    /// <para type="description">
    /// Adds a new Software-Defined Networking VNet (virtual network) to the specified SDN zone.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveSdnVnet", SupportsShouldProcess = true)]
    public class NewPveSdnVnetCmdlet : PveCmdletBase
    {
        /// <summary>The VNet identifier (alphanumeric, up to 8 characters).</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Vnet { get; set; } = string.Empty;

        /// <summary>The SDN zone this VNet belongs to.</summary>
        [Parameter(Mandatory = true, Position = 1)]
        public string Zone { get; set; } = string.Empty;

        /// <summary>VLAN tag for VLAN-type zones.</summary>
        [Parameter(Mandatory = false)]
        public int? Tag { get; set; }

        /// <summary>Optional alias/description for the VNet.</summary>
        [Parameter(Mandatory = false)]
        public string? Alias { get; set; }

        /// <summary>Enable VLAN awareness on this VNet.</summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter VlanAware { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"{Vnet} in zone {Zone}", "Create PVE SDN VNet"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            var data = new Dictionary<string, string>
            {
                ["vnet"] = Vnet,
                ["zone"] = Zone
            };

            if (Tag.HasValue)                   data["tag"]       = Tag.Value.ToString();
            if (!string.IsNullOrEmpty(Alias))   data["alias"]     = Alias;
            if (VlanAware.IsPresent)             data["vlanaware"] = "1";

            client.PostAsync("/cluster/sdn/vnets", data).GetAwaiter().GetResult();
        }
    }
}
