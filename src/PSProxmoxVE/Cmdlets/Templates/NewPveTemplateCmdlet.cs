using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Templates
{
    /// <summary>
    /// <para type="synopsis">Converts a Proxmox VE virtual machine to a template.</para>
    /// <para type="description">
    /// Converts the specified VM into a template. This is a one-way operation — the VM
    /// will no longer be directly runnable, but can be cloned via New-PveVmFromTemplate.
    /// The VM must be stopped before converting. Returns a PveTask.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveTemplate", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public class NewPveTemplateCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier to convert. Accepts pipeline input from Get-PveVm (PveVm.VmId).</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>When specified, waits for the conversion task to complete before returning.</summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            var session  = GetSession();

            if (!ShouldProcess($"VM {VmId} on {Node}", "Convert to PVE Template (irreversible)"))
                return;
            var service  = new TemplateService();
            var task     = service.CreateTemplate(session, Node, VmId);

            if (Wait.IsPresent && !string.IsNullOrEmpty(task.Upid))
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid);
            }

            WriteObject(task);
        }
    }
}
