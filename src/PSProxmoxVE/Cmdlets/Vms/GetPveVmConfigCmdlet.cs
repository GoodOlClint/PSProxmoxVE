using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Gets the configuration of a QEMU/KVM virtual machine.</para>
    /// <para type="description">
    /// Retrieves the full hardware and metadata configuration of the specified virtual machine
    /// from the Proxmox VE API, including CPU, memory, disk, network, and cloud-init settings.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveVmConfig")]
    [OutputType(typeof(PveVmConfig))]
    public sealed class GetPveVmConfigCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM whose configuration to retrieve. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var vmService = new VmService();

            var config = vmService.GetVmConfig(session, Node, VmId);
            WriteObject(config);
        }
    }
}
