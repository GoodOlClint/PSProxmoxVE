using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Resets (hard-resets) a QEMU/KVM virtual machine on a Proxmox VE node.</para>
    /// <para type="description">
    /// Sends a hard-reset signal to the specified virtual machine via the Proxmox VE API.
    /// This is equivalent to pressing the physical reset button on a machine — no graceful
    /// shutdown occurs. Use Restart-PveVm for a graceful reboot.
    /// Use -Wait to block until the reset task completes.
    /// </para>
    /// </summary>
    [Cmdlet("Reset", "PveVm", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class ResetPveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM to reset. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the reset task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", "Reset-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            var task = vmService.ResetVm(session, Node, VmId);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
