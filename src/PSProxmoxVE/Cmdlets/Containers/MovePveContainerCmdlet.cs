using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Migrates an LXC container to a different Proxmox VE node.</para>
    /// <para type="description">
    /// Performs a live or offline migration of the specified container to the target node.
    /// Use -Online for live migration (container remains running). Use -Wait to block until the
    /// migration task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "PveContainer", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class MovePveContainerCmdlet : PveCmdletBase
    {
        /// <summary>The node on which the container currently resides.</summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The ID of the container to migrate. Accepts pipeline input.</summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>The destination node to migrate the container to.</summary>
        [Parameter(Mandatory = true, HelpMessage = "The destination node for migration.")]
        public string TargetNode { get; set; } = string.Empty;

        /// <summary>
        /// When specified, performs a live migration so the container remains running during migration.
        /// Requires shared storage between the source and target nodes.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Perform live migration (container stays running).")]
        public SwitchParameter Online { get; set; }

        /// <summary>When specified, waits for the migration task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} from node '{Node}' to node '{TargetNode}'", "Move-PveContainer"))
                return;

            var session = GetSession();
            var service = new ContainerService();

            WriteVerbose($"Migrating container {VmId} from '{Node}' to '{TargetNode}'...");
            var task = service.MigrateContainer(session, Node, VmId, TargetNode, Online.IsPresent);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
