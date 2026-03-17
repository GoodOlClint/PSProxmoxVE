using System;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Vms;

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
    public class WaitPveTaskCmdlet : PveCmdletBase
    {
        /// <summary>The node on which the task is running.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// The UPID of the task to wait for. Accepts pipeline input from PveTask (PveTask.Upid).
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public string Upid { get; set; } = string.Empty;

        /// <summary>
        /// Maximum time to wait for the task. Defaults to no timeout.
        /// Example: -Timeout (New-TimeSpan -Minutes 10)
        /// </summary>
        [Parameter(Mandatory = false)]
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// How frequently to poll the task status. Defaults to 2 seconds.
        /// Example: -PollInterval (New-TimeSpan -Seconds 5)
        /// </summary>
        [Parameter(Mandatory = false)]
        public TimeSpan? PollInterval { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            using var client = new PveHttpClient(session);

            var poll     = PollInterval ?? TimeSpan.FromSeconds(2);
            var deadline = Timeout.HasValue ? DateTime.UtcNow + Timeout.Value : DateTime.MaxValue;

            var encodedUpid  = Uri.EscapeDataString(Upid);
            var statusResource = $"/nodes/{Node}/tasks/{encodedUpid}/status";

            // Derive a short human-readable description from the UPID for progress display
            var taskDesc = Upid.Length > 50 ? Upid.Substring(0, 47) + "..." : Upid;

            var activityId     = Math.Abs(Upid.GetHashCode()) % 1000 + 1;
            var progressRecord = new ProgressRecord(activityId, $"Waiting for task on {Node}", taskDesc)
            {
                PercentComplete = -1  // indeterminate
            };

            try
            {
                while (true)
                {
                    if (DateTime.UtcNow >= deadline)
                        throw new PveTaskTimeoutException(Upid, Timeout!.Value);

                    WriteProgress(progressRecord);

                    var elapsed = Timeout.HasValue
                        ? (int)((DateTime.UtcNow - (deadline - Timeout.Value)).TotalSeconds)
                        : -1;
                    if (Timeout.HasValue && elapsed >= 0)
                    {
                        var totalSecs = (int)Timeout.Value.TotalSeconds;
                        progressRecord.PercentComplete = totalSecs > 0
                            ? Math.Min(99, (elapsed * 100) / totalSecs)
                            : -1;
                        progressRecord.SecondsRemaining = Math.Max(0, totalSecs - elapsed);
                    }

                    System.Threading.Thread.Sleep((int)poll.TotalMilliseconds);

                    var statusJson = client.GetAsync(statusResource).GetAwaiter().GetResult();
                    var statusRoot = JObject.Parse(statusJson);
                    var data       = statusRoot["data"];

                    var status     = data?["status"]?.ToString();
                    var exitStatus = data?["exitstatus"]?.ToString();

                    if (status == "stopped")
                    {
                        progressRecord.RecordType = ProgressRecordType.Completed;
                        WriteProgress(progressRecord);

                        var task = new PveTask
                        {
                            Upid       = Upid,
                            Node       = Node,
                            Status     = status,
                            ExitStatus = exitStatus
                        };

                        if (!task.IsSuccessful && !string.IsNullOrEmpty(exitStatus) && exitStatus != "OK")
                            throw new PveTaskFailedException(Upid, exitStatus);

                        WriteObject(task);
                        return;
                    }
                }
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
                ThrowTerminatingError(new ErrorRecord(
                    ex, "PveTaskFailed", ErrorCategory.OperationStopped, Upid));
            }
        }
    }
}
