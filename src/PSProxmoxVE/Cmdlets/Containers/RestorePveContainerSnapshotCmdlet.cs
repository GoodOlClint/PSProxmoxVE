using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Rolls back a Proxmox VE container to a snapshot.</para>
    /// <para type="description">
    /// Restores the container state to the specified snapshot, discarding all changes made since
    /// the snapshot was taken. This is a destructive operation. Returns a PveTask.
    /// Use -Wait to block until rollback completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsData.Restore, "PveContainerSnapshot",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class RestorePveContainerSnapshotCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The container identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// The snapshot name to roll back to. Accepts pipeline input from Get-PveContainerSnapshot (PveSnapshot.Name).
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, ValueFromPipelineByPropertyName = true, HelpMessage = "The snapshot name to roll back to.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>When specified, waits for the rollback task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessPveRecord()
        {
            var session = GetSession();

            if (!ShouldProcess($"Container {VmId} on {Node}", $"Restore snapshot '{Name}' (current state will be lost)"))
                return;

            WriteVerbose($"Restoring snapshot '{Name}' on container {VmId}...");
            var service = new ContainerService();
            var task = service.RollbackContainerSnapshot(session, Node, VmId, Name);

            if (Wait.IsPresent && !string.IsNullOrEmpty(task.Upid))
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid);
            }

            WriteObject(task);
        }
    }
}
