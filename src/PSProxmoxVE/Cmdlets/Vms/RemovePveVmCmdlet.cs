using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Removes a QEMU/KVM virtual machine from a Proxmox VE node.</para>
    /// <para type="description">
    /// Deletes a virtual machine and, optionally, all associated disk images.
    /// This operation is destructive and requires confirmation.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveVm",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class RemovePveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM to remove. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">
        /// When specified, also removes the VM from HA resource configuration and replication jobs.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Remove all associated resources.")]
        public SwitchParameter Purge { get; set; }

        /// <summary>
        /// <para type="description">
        /// When specified, sends skiplock=1 to PVE to bypass locks. PVE honours this parameter for root@pam only; non-root callers will receive a 403 permission error.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Bypass locks via skiplock=1 (root@pam only).")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the removal task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessPveRecord()
        {
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", "Remove-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            WriteVerbose($"Removing VM {VmId} from node '{Node}'...");
            var task = vmService.RemoveVm(session, Node, VmId, Purge.IsPresent, Force.IsPresent);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid);
            }

            WriteObject(task);
        }
    }
}
