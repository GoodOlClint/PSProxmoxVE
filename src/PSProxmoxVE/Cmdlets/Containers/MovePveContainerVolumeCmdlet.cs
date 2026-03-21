using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Moves a container volume to a different storage.</para>
    /// <para type="description">
    /// Moves the specified volume on an LXC container to a different storage backend
    /// via the Proxmox VE API. Optionally deletes the original volume after a successful move.
    /// Use -Wait to block until the move task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "PveContainerVolume", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class MovePveContainerVolumeCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the container resides.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container whose volume should be moved.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The volume to move (e.g., "rootfs", "mp0").</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The volume to move (e.g. rootfs, mp0).")]
        public string Volume { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The target storage to move the volume to.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The target storage identifier.")]
        public string Storage { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">When specified, deletes the original volume after a successful move. Default is true.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Delete the original volume after move (default true).")]
        public SwitchParameter Delete { get; set; } = true;

        /// <summary>
        /// <para type="description">When specified, waits for the move task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Volume '{Volume}' on container {VmId} (node '{Node}') to storage '{Storage}'", "Move-PveContainerVolume"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Moving volume '{Volume}' on container {VmId} to storage '{Storage}'...");
            var task = containerService.MoveVolume(session, Node, VmId, Volume, Storage, Delete.IsPresent);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
