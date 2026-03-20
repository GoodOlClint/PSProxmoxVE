using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Suspends a QEMU/KVM virtual machine on a Proxmox VE node.</para>
    /// <para type="description">
    /// Suspends (pauses) the specified virtual machine via the Proxmox VE API.
    /// The VM state is preserved in memory. Use Resume-PveVm to resume execution.
    /// Use -Wait to block until the suspend task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Suspend, "PveVm", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class SuspendPveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM to suspend. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the suspend task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", "Suspend-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            WriteVerbose($"Suspending VM {VmId} on node '{Node}'...");
            var task = vmService.SuspendVm(session, Node, VmId);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
