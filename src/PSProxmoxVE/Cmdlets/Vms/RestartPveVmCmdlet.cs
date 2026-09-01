using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Gracefully restarts a QEMU/KVM virtual machine on a Proxmox VE node.</para>
    /// <para type="description">
    /// Reboots the VM through Proxmox VE's native reboot endpoint, which shuts the guest down
    /// and starts it again as a single server-side operation. A configurable timeout controls
    /// how long to wait for the guest to shut down cleanly. Use -Wait to block until the VM is
    /// running again.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Restart, "PveVm", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class RestartPveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM to restart. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">
        /// Timeout in seconds for the graceful shutdown phase and -Wait status polling. Defaults to 60 seconds.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Timeout in seconds for -Wait (default 60).")]
        [ValidateRange(1, 3600)]
        public int Timeout { get; set; } = 60;

        /// <summary>
        /// <para type="description">When specified, waits until the VM is running again before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", "Restart-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            WriteVerbose($"Restarting VM {VmId} on node '{Node}'...");

            var task = vmService.RebootVm(session, Node, VmId, Timeout);

            if (Wait.IsPresent)
                task = WaitForStatusTransition(session, Node, task, VmId, "running", Timeout);

            WriteObject(task);
        }
    }
}
