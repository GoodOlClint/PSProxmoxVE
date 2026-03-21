using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Gets filesystem information from the QEMU guest agent.</para>
    /// <para type="description">
    /// Queries the QEMU guest agent running inside the specified VM for its filesystem
    /// information, including mount points, filesystem types, and usage statistics.
    /// The guest agent must be installed and running inside the VM.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveVmGuestFsInfo")]
    [OutputType(typeof(PveGuestFsInfo))]
    public sealed class GetPveVmGuestFsInfoCmdlet : PveCmdletBase
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

            WriteVerbose($"Getting guest filesystem info for VM {VmId}...");
            var service = new VmService();
            var fsInfos = service.GetGuestFsInfo(session, Node, VmId);

            foreach (var fs in fsInfos)
                WriteObject(fs);
        }
    }
}
