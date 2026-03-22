using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Templates
{
    /// <summary>
    /// <para type="synopsis">Removes a Proxmox VE VM template.</para>
    /// <para type="description">
    /// Permanently deletes the specified VM template and all its associated disk images.
    /// This action is irreversible. Returns a PveTask.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveTemplate",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class RemovePveTemplateCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM/template identifier to remove. Accepts pipeline input from Get-PveVm (PveVm.VmId).</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>When specified, also removes all associated backup files and jobs.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Remove all associated resources.")]
        public SwitchParameter Purge { get; set; }

        /// <summary>When specified, waits for the deletion task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Template {VmId} on {Node}", "Remove PVE Template (all disks will be deleted)"))
                return;

            var session  = GetSession();
            var service  = new TemplateService();

            WriteVerbose($"Removing template {VmId}...");
            var task     = service.RemoveTemplate(session, Node, VmId, Purge.IsPresent);

            if (Wait.IsPresent && !string.IsNullOrEmpty(task.Upid))
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid);
            }

            WriteObject(task);
        }
    }
}
