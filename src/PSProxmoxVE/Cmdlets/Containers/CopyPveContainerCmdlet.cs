using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Clones (copies) an LXC container on a Proxmox VE node.</para>
    /// <para type="description">
    /// Creates a clone of an existing LXC container. By default, a linked clone is created
    /// when the source is a template. Use -Full to create a full independent copy. Optionally
    /// specify a target node to create the clone on a different cluster node.
    /// Use -Wait to block until the clone task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Copy, "PveContainer", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class CopyPveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the source container resides.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The node where the source container resides.")]
        public string SourceNode { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the source container to clone. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The container ID to assign to the new clone. When omitted, the next available ID is used.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Container ID for the new clone.")]
        [ValidateRange(100, 999999999)]
        public int? NewVmId { get; set; }

        /// <summary>
        /// <para type="description">The hostname for the new clone.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Hostname for the new clone.")]
        public string? NewName { get; set; }

        /// <summary>
        /// <para type="description">The target node for the clone. Defaults to the source node.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Target node for the clone.")]
        public string? TargetNode { get; set; }

        /// <summary>
        /// <para type="description">
        /// When specified, creates a full independent clone instead of a linked clone.
        /// A full clone is required when the source container is not a template.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Perform a full (non-linked) clone.")]
        public SwitchParameter Full { get; set; }

        /// <summary>
        /// <para type="description">Target storage pool for the full clone root filesystem.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The storage pool name.")]
        public string? Storage { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the clone task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            var target = TargetNode ?? SourceNode;
            if (!ShouldProcess($"Container {VmId} on node '{SourceNode}' to new container on node '{target}'", "Copy-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Cloning container {VmId}...");
            var task = containerService.CloneContainer(
                session,
                SourceNode,
                VmId,
                NewVmId ?? 0,
                NewName,
                TargetNode,
                Full.IsPresent);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, task.Node ?? SourceNode, task.Upid!, null, null, null);
            }

            WriteObject(task);
        }
    }
}
