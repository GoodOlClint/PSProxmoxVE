using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Tasks
{
    /// <summary>
    /// <para type="synopsis">Stops a running task on a Proxmox VE node.</para>
    /// <para type="description">
    /// Cancels a running task identified by its UPID on the specified node.
    /// This is a destructive operation and requires confirmation.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "PveTask",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    public sealed class StopPveTaskCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the task is running.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The UPID of the task to stop.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The task UPID.")]
        public string Upid { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"task '{Upid}' on node '{Node}'", "Stop-PveTask"))
                return;

            var session = GetSession();
            var service = new TaskService();

            WriteVerbose($"Stopping task '{Upid}' on node '{Node}'...");
            service.StopTask(session, Node, Upid);
        }
    }
}
