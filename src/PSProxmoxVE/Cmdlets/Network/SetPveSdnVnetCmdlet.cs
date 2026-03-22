using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Updates an SDN VNet configuration in Proxmox VE.</para>
    /// <para type="description">
    /// Modifies the specified Software-Defined Networking VNet configuration.
    /// Changes are pending until Invoke-PveSdnApply is called.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveSdnVnet", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveSdnVnetCmdlet : PveCmdletBase
    {
        /// <summary>The VNet identifier.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The SDN VNet name.")]
        public string Vnet { get; set; } = string.Empty;

        /// <summary>An alias for the VNet.</summary>
        [Parameter(Mandatory = false, HelpMessage = "An alias for the VNet.")]
        public string? Alias { get; set; }

        /// <summary>The zone this VNet belongs to.</summary>
        [Parameter(Mandatory = false, HelpMessage = "The zone this VNet belongs to.")]
        public string? Zone { get; set; }

        /// <summary>VLAN tag for this VNet.</summary>
        [Parameter(Mandatory = false, HelpMessage = "VLAN tag for this VNet.")]
        public int? Tag { get; set; }

        /// <summary>Enable VLAN-aware bridging.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Enable VLAN-aware bridging.")]
        public SwitchParameter VlanAware { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Vnet, "Set PVE SDN VNet"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);
            var service = new NetworkService();

            WriteVerbose($"Updating SDN VNet '{Vnet}'...");
            var config = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(Alias)) config["alias"] = Alias!;
            if (!string.IsNullOrEmpty(Zone))  config["zone"]  = Zone!;
            if (MyInvocation.BoundParameters.ContainsKey("Tag"))
                config["tag"] = Tag!.Value.ToString();
            if (MyInvocation.BoundParameters.ContainsKey("VlanAware"))
                config["vlanaware"] = VlanAware.IsPresent ? "1" : "0";

            service.UpdateSdnVnet(session, Vnet, config);
        }
    }
}
