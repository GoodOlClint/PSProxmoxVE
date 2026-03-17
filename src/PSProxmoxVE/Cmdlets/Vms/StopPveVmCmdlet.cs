using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Stops (powers off) a QEMU/KVM virtual machine on a Proxmox VE node.</para>
    /// <para type="description">
    /// Sends a stop command to the specified virtual machine via the Proxmox VE API.
    /// This is equivalent to pulling the power plug — the VM is stopped immediately without
    /// a graceful shutdown. Use Restart-PveVm or a guest-initiated shutdown for graceful stops.
    /// Use -Wait to block until the stop task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "PveVm", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class StopPveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM to stop. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the stop task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", "Stop-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            var task = vmService.StopVm(session, Node, VmId);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, task.Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
