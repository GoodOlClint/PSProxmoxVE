using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Resumes a suspended QEMU/KVM virtual machine on a Proxmox VE node.</para>
    /// <para type="description">
    /// Resumes execution of a previously suspended virtual machine via the Proxmox VE API.
    /// Use -Wait to block until the resume task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Resume, "PveVm", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class ResumePveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM to resume. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the resume task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        /// <summary>Maximum seconds to wait for the status transition when -Wait is specified. Default 60.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Timeout in seconds for -Wait (default 60).")]
        [ValidateRange(1, 3600)]
        public int Timeout { get; set; } = 60;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", "Resume-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            WriteVerbose($"Resuming VM {VmId} on node '{Node}'...");
            var task = vmService.ResumeVm(session, Node, VmId);

            if (Wait.IsPresent)
            {
                task = WaitForStatusTransition(session, Node, task, VmId, "running", Timeout);
            }

            WriteObject(task);
        }
    }
}
