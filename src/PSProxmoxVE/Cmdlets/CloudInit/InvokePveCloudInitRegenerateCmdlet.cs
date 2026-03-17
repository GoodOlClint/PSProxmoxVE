using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

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
    public class InvokePveCloudInitRegenerateCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier. Accepts pipeline input from Get-PveVm (PveVm.VmId).</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on {Node}", "Regenerate PVE Cloud-Init Drive"))
                return;

            var session = GetSession();
            var service = new CloudInitService();
            service.RegenerateCloudInitImage(session, Node, VmId);

            var task = new PveTask
            {
                Node       = Node,
                Status     = "stopped",
                ExitStatus = "OK"
            };

            WriteObject(task);
        }
    }
}
