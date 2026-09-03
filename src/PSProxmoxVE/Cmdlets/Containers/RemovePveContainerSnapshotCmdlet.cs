using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Removes a snapshot from a Proxmox VE container.</para>
    /// <para type="description">
    /// Deletes the specified snapshot from the LXC container.
    /// Returns a PveTask. Use -Wait to block until removal completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveContainerSnapshot", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class RemovePveContainerSnapshotCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The container identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// The snapshot name to remove. Accepts pipeline input from Get-PveContainerSnapshot (PveSnapshot.Name).
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, ValueFromPipelineByPropertyName = true, HelpMessage = "The snapshot name to remove.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>When specified, waits for the removal task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            if (!ShouldProcess($"Container {VmId} snapshot '{Name}' on {Node}", "Remove container snapshot"))
                return;

            WriteVerbose($"Removing snapshot '{Name}' from container {VmId}...");
            var service = new ContainerService();
            var task = service.RemoveContainerSnapshot(session, Node, VmId, Name);

            if (Wait.IsPresent && !string.IsNullOrEmpty(task.Upid))
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid);
            }

            WriteObject(task);
        }
    }
}
