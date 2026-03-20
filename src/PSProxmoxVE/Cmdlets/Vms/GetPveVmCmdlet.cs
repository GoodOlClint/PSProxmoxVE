using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Gets one or more QEMU/KVM virtual machines from a Proxmox VE server.</para>
    /// <para type="description">
    /// Retrieves virtual machine objects from the Proxmox VE API. Results can be filtered by
    /// node, VM ID, name, status, tag, or limited to template VMs only.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveVm")]
    [OutputType(typeof(PveVm))]
    public sealed class GetPveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The name of the node to query. Accepts pipeline input from a PveNode object's Name property.
        /// When omitted, VMs from all nodes are returned.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string? Node { get; set; }

        /// <summary>
        /// <para type="description">Filter results to the VM with this ID.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        /// <summary>
        /// <para type="description">Filter results to VMs whose name matches this value (case-insensitive, contains match).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by name.")]
        public string? Name { get; set; }

        /// <summary>
        /// <para type="description">Filter results to VMs in the specified status (e.g., "running", "stopped").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by status (e.g. running, stopped).")]
        public string? Status { get; set; }

        /// <summary>
        /// <para type="description">Filter results to VMs that have the specified tag (substring match against the semicolon-separated tags field).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by tag.")]
        public string? Tag { get; set; }

        /// <summary>
        /// <para type="description">When specified, returns only VMs that are marked as templates.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Return only VMs marked as templates.")]
        public SwitchParameter TemplatesOnly { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose("Getting VMs...");
            var service = new VmService();

            IEnumerable<PveVm> vms = service.GetVms(session, Node);

            if (VmId.HasValue)
                vms = vms.Where(v => v.VmId == VmId.Value);

            if (!string.IsNullOrEmpty(Name))
                vms = vms.Where(v => v.Name != null &&
                    v.Name.IndexOf(Name, System.StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrEmpty(Status))
                vms = vms.Where(v => string.Equals(v.Status, Status, System.StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(Tag))
                vms = vms.Where(v => v.Tags != null &&
                    v.Tags.IndexOf(Tag, System.StringComparison.OrdinalIgnoreCase) >= 0);

            if (TemplatesOnly.IsPresent)
                vms = vms.Where(v => v.Template == 1);

            foreach (var vm in vms)
                WriteObject(vm);
        }
    }
}
