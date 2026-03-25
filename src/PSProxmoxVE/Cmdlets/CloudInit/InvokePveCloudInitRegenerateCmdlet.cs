using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE.Cmdlets.CloudInit
{
    /// <summary>
    /// <para type="synopsis">Regenerates the cloud-init drive on a Proxmox VE virtual machine.</para>
    /// <para type="description">
    /// Forces regeneration of the cloud-init ISO drive on the specified VM, ensuring that
    /// the latest cloud-init configuration is applied on the next boot. This is useful
    /// after making changes via Set-PveCloudInitConfig. Returns a PveTask.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "PveCloudInitRegenerate", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class InvokePveCloudInitRegenerateCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier. Accepts pipeline input from Get-PveVm (PveVm.VmId).</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>When specified, waits for the regeneration task to complete before returning.</summary>
        [Parameter(HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on {Node}", "Regenerate PVE Cloud-Init Drive"))
                return;

            var session = GetSession();
            RequireVersion(session, "Cloud-Init management", 7, 2);

            WriteVerbose($"Regenerating cloud-init drive for VM {VmId}...");
            var service = new CloudInitService();
            var upid = service.RegenerateCloudInitImage(session, Node, VmId);

            var task = new PveTask { Upid = upid, Node = Node, Status = "running" };

            if (Wait.IsPresent && !string.IsNullOrEmpty(upid))
            {
                task = new TaskService().WaitForTask(session, Node, upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
