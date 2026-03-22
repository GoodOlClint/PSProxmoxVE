using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Templates
{
    /// <summary>
    /// <para type="synopsis">Clones a Proxmox VE VM template to create a new virtual machine.</para>
    /// <para type="description">
    /// Creates a new VM by cloning the specified template. Supports both linked clones
    /// (fast, space-efficient, requires template on shared storage) and full clones
    /// (-Full switch). The source VM/template ID can be piped from Get-PveTemplate.
    /// Returns a PveTask.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveVmFromTemplate", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class NewPveVmFromTemplateCmdlet : PveCmdletBase
    {
        /// <summary>The node where the source template resides. Alias: Node.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The node where the template resides.")]
        [Alias("Node")]
        public string TemplateNode { get; set; } = string.Empty;

        /// <summary>The source template VM ID. Accepts pipeline input from Get-PveTemplate (PveVm.VmId).</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>The VM ID for the new cloned VM.</summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "VM ID for the new cloned VM.")]
        [ValidateRange(100, 999999999)]
        public int NewVmId { get; set; }

        /// <summary>Optional name for the new VM.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Name for the new VM.")]
        public string? NewName { get; set; }

        /// <summary>
        /// The target node for the new VM. When omitted, the new VM is created on the same node
        /// as the template.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Target node for the new VM.")]
        public string? TargetNode { get; set; }

        /// <summary>
        /// When specified, creates a full clone (independent copy of all disks).
        /// When omitted, creates a linked clone (shares base disk with template).
        /// Linked clones require the template to reside on shared storage.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Perform a full (non-linked) clone.")]
        public SwitchParameter Full { get; set; }

        /// <summary>When specified, waits for the clone task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Template {VmId} -> new VM {NewVmId}", "Clone PVE VM from Template"))
                return;

            var session = GetSession();
            var service = new VmService();

            WriteVerbose($"Cloning VM from template {VmId}...");
            var task    = service.CloneVm(
                session,
                TemplateNode,
                VmId,
                NewVmId,
                name:       NewName,
                targetNode: TargetNode,
                full:       Full.IsPresent);

            if (Wait.IsPresent && !string.IsNullOrEmpty(task.Upid))
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, TemplateNode, task.Upid);
            }

            WriteObject(task);
        }
    }
}
