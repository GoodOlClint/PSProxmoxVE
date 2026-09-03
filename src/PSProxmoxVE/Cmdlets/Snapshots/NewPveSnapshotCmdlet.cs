using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Snapshots
{
    /// <summary>
    /// <para type="synopsis">Creates a snapshot of a Proxmox VE virtual machine.</para>
    /// <para type="description">
    /// Takes a snapshot of the specified VM. Optionally includes the VM RAM state.
    /// Returns a PveTask. Use -Wait to block until the snapshot completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveSnapshot", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class NewPveSnapshotCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier. Accepts pipeline input from Get-PveVm (PveVm.VmId).</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>The snapshot name (alphanumeric, hyphens and underscores).</summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "The snapshot name.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional human-readable description for the snapshot.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Description for the snapshot.")]
        public string? Description { get; set; }

        /// <summary>When specified, includes the VM memory state in the snapshot.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Include VM memory state in the snapshot.")]
        public SwitchParameter IncludeVmState { get; set; }

        /// <summary>When specified, waits for the snapshot task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessPveRecord()
        {
            if (!ShouldProcess($"VM {VmId} on {Node}", $"Create snapshot '{Name}'"))
                return;

            var session = GetSession();

            WriteVerbose($"Creating snapshot '{Name}' for VM {VmId}...");
            var service = new SnapshotService();
            var task = service.CreateSnapshot(session, Node, VmId, Name, Description, IncludeVmState.IsPresent);

            if (Wait.IsPresent && !string.IsNullOrEmpty(task.Upid))
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid);
            }

            WriteObject(task);
        }
    }
}
