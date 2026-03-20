using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Gets network interface information from the QEMU guest agent.</para>
    /// <para type="description">
    /// Queries the QEMU guest agent running inside the specified VM for its network
    /// interface configuration, including interface names, MAC addresses, and IP addresses.
    /// The guest agent must be installed and running inside the VM.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveVmGuestNetwork")]
    [OutputType(typeof(PveGuestNetworkInterface))]
    public sealed class GetPveVmGuestNetworkCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose($"Getting guest network interfaces for VM {VmId}...");
            var service = new VmService();
            var interfaces = service.GetGuestNetworkInterfaces(session, Node, VmId);

            foreach (var iface in interfaces)
                WriteObject(iface);
        }
    }
}
