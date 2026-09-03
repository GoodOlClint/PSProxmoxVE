using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Removes a network interface definition from a Proxmox VE node.</para>
    /// <para type="description">
    /// Deletes the specified network interface configuration from the node. After removing,
    /// use Invoke-PveNetworkApply to apply pending changes to the running system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveNetwork", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveNetworkCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The interface name to remove.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The network interface name.")]
        public string Iface { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"{Iface} on {Node}", "Remove PVE Network Iface"))
                return;

            var session = GetSession();

            WriteVerbose($"Removing network interface '{Iface}' from node '{Node}'...");
            var service = new NetworkService();
            service.RemoveNetwork(session, Node, Iface);
        }
    }
}
