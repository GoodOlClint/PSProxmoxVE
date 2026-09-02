using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Tasks
{
    /// <summary>
    /// <para type="synopsis">Waits for a Proxmox VE task to complete.</para>
    /// <para type="description">
    /// Polls the specified task until it reaches the "stopped" state or the timeout elapses.
    /// Reports progress via Write-Progress. UPID can be piped from any cmdlet that returns
    /// a PveTask (PveTask.Upid). Throws PveTaskFailedException if the task exits with an
    /// error status, and PveTaskTimeoutException if the timeout elapses.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Wait, "PveTask")]
    [OutputType(typeof(PveTask))]
    public sealed class WaitPveTaskCmdlet : PveCmdletBase
    {
        /// <summary>
        /// TaskService.WaitForTask treats a null timeout as "use its own 10-minute default",
        /// not "wait forever" — this cmdlet's own contract is the latter, so an omitted
        /// -Timeout is passed through as this instead of null.
        /// </summary>
        private static readonly TimeSpan NoTimeout = TimeSpan.FromDays(36500);

        /// <summary>The node on which the task is running.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// The UPID of the task to wait for. Accepts pipeline input from PveTask (PveTask.Upid).
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The task UPID.")]
        public string Upid { get; set; } = string.Empty;

        /// <summary>
        /// Maximum time to wait for the task. Defaults to no timeout.
        /// Example: -Timeout (New-TimeSpan -Minutes 10)
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Maximum time to wait for the task.")]
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// How frequently to poll the task status. Defaults to 2 seconds.
        /// Example: -PollInterval (New-TimeSpan -Seconds 5)
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "How often to poll task status.")]
        public TimeSpan? PollInterval { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            using var client = new PveHttpClient(session);
            var taskService = new TaskService(client);

            var activityId = Math.Abs(Upid.GetHashCode()) % 1000 + 1;
            var taskDesc = Upid.Length > 50 ? Upid.Substring(0, 47) + "..." : Upid;
            var progressRecord = new ProgressRecord(activityId, $"Waiting for task on {Node}", taskDesc)
            {
                PercentComplete = -1
            };

            var startedAt = DateTime.UtcNow;

            void ReportProgress(PveTask task)
            {
                if (Timeout.HasValue)
                {
                    var totalSecs = (int)Timeout.Value.TotalSeconds;
                    var elapsed = (int)(DateTime.UtcNow - startedAt).TotalSeconds;
                    progressRecord.PercentComplete = totalSecs > 0
                        ? Math.Min(99, (elapsed * 100) / totalSecs)
                        : -1;
                    progressRecord.SecondsRemaining = Math.Max(0, totalSecs - elapsed);
                }
                WriteProgress(progressRecord);
            }

            try
            {
                var task = taskService.WaitForTask(session, Node, Upid, Timeout ?? NoTimeout, PollInterval,
                    ReportProgress);

                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);

                WriteObject(task);
            }
            catch (PveTaskTimeoutException ex)
            {
                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);
                ThrowTerminatingError(new ErrorRecord(
                    ex, "PveTaskTimeout", ErrorCategory.OperationTimeout, Upid));
            }
            catch (PveTaskFailedException ex)
            {
                progressRecord.RecordType = ProgressRecordType.Completed;
                WriteProgress(progressRecord);
                ThrowTerminatingError(new ErrorRecord(
                    ex, "PveTaskFailed", ErrorCategory.OperationStopped, Upid));
            }
        }
    }
}
