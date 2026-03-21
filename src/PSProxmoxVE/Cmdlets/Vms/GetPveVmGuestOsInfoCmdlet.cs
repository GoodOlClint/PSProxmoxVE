using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Gets OS information from the QEMU guest agent.</para>
    /// <para type="description">
    /// Queries the QEMU guest agent running inside the specified VM for its operating system
    /// information, including OS name, kernel version, and machine type.
    /// The guest agent must be installed and running inside the VM.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveVmGuestOsInfo")]
    [OutputType(typeof(PveGuestOsInfo))]
    public sealed class GetPveVmGuestOsInfoCmdlet : PveCmdletBase
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

            WriteVerbose($"Getting guest OS info for VM {VmId}...");
            var service = new VmService();
            var osInfo = service.GetGuestOsInfo(session, Node, VmId);

            if (osInfo != null)
                WriteObject(osInfo);
        }
    }
}
