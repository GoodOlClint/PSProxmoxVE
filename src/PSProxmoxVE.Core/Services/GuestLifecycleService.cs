using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Issues guest (VM/container) lifecycle operations and waits for them to actually take
    /// effect: past the task PVE returns, and past the guest config lock the task's completion
    /// does not account for. See docs/decisions/ ADR 0015 (the config-lock wait) and ADR 0020
    /// (the flock retry <see cref="GuestLockRetry"/> implements).
    /// </summary>
    public class GuestLifecycleService : PveServiceBase
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        private readonly Func<TimeSpan, Task> _pollDelay;

        /// <summary>Initializes a new instance that creates its own HTTP clients.</summary>
        public GuestLifecycleService() : this(Sleep) { }

        /// <summary>Initializes a new instance that uses the supplied HTTP client for all requests.</summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        public GuestLifecycleService(IPveHttpClient client) : this(client, Sleep) { }

        /// <summary>
        /// Test seam: same as <see cref="GuestLifecycleService()"/> but with the wait between
        /// status polls replaceable, so a test can drive the loop without sleeping for it.
        /// </summary>
        /// <param name="pollDelay">Invoked with each poll interval instead of sleeping.</param>
        internal GuestLifecycleService(Func<TimeSpan, Task> pollDelay)
        {
            _pollDelay = pollDelay ?? throw new ArgumentNullException(nameof(pollDelay));
        }

        /// <summary>
        /// Test seam: same as <see cref="GuestLifecycleService(IPveHttpClient)"/> but with the
        /// wait between status polls replaceable.
        /// </summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        /// <param name="pollDelay">Invoked with each poll interval instead of sleeping.</param>
        internal GuestLifecycleService(IPveHttpClient client, Func<TimeSpan, Task> pollDelay) : base(client)
        {
            _pollDelay = pollDelay ?? throw new ArgumentNullException(nameof(pollDelay));
        }

        private static Task Sleep(TimeSpan duration)
        {
            Thread.Sleep(duration);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Issues a guest operation and waits for the task it returns, reissuing the pair while
        /// PVE rejects it for the guest's config flock.
        ///
        /// PVE takes that flock inside the worker for most guest operations, so the failure
        /// surfaces as a failed task rather than a failed request and cannot be retried at the
        /// HTTP layer. <c>lock_config</c> raises it before doing any work, so a reissue repeats
        /// nothing.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The node the task runs on.</param>
        /// <param name="issueOperation">Issues the API call; invoked again on each retry.</param>
        /// <param name="onProgress">
        ///   Invoked with a progress message before each reissue. A caller with somewhere to
        ///   report progress should pass one — a wait this long is otherwise indistinguishable
        ///   from a hang.
        /// </param>
        /// <returns>The completed task, or the issued task when the call returned no UPID.</returns>
        public PveTask InvokeGuestTask(
            PveSession session,
            string node,
            Func<PveTask> issueOperation,
            Action<string>? onProgress = null)
        {
            if (issueOperation == null) throw new ArgumentNullException(nameof(issueOperation));

            return Invoke(session, client =>
            {
                var taskService = new TaskService(client);
                return GuestLockRetry.Execute(
                    () =>
                    {
                        var task = issueOperation();
                        return string.IsNullOrEmpty(task.Upid)
                            ? task
                            : taskService.WaitForTask(session, node, task.Upid);
                    },
                    onRetry: ex => onProgress?.Invoke($"Guest is locked, retrying: {ex.Message}"));
            });
        }

        /// <summary>
        /// Waits for a PVE task to complete, then polls guest status until it matches
        /// <paramref name="expectedStatus"/> and its config lock has cleared. Used by lifecycle
        /// cmdlets (Start, Stop, Suspend, Resume, etc.) when -Wait is specified.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="issueOperation">
        ///   Issues the lifecycle API call. Invoked again on each retry, so it must be safe to
        ///   repeat — see <see cref="InvokeGuestTask"/>.
        /// </param>
        /// <param name="vmid">The VM or container ID to poll.</param>
        /// <param name="expectedStatus">The expected status string (e.g. "running", "stopped", "paused").</param>
        /// <param name="timeoutSeconds">Maximum seconds to wait for the status transition. Default 60.</param>
        /// <param name="isContainer">True to poll container status instead of VM status.</param>
        /// <param name="onProgress">
        ///   Invoked with a progress message when a status poll fails and is retried, and
        ///   forwarded to <see cref="InvokeGuestTask"/> for its own retry reporting.
        /// </param>
        /// <returns>The completed task.</returns>
        public PveTask WaitForStatusTransition(
            PveSession session,
            string node,
            Func<PveTask> issueOperation,
            int vmid,
            string expectedStatus,
            int timeoutSeconds = 60,
            bool isContainer = false,
            Action<string>? onProgress = null)
        {
            var task = InvokeGuestTask(session, node, issueOperation, onProgress);

            // We query the status/current endpoint directly instead of the list endpoint
            // because it returns qmpstatus (needed for paused state detection — PVE reports
            // status=running but qmpstatus=paused for suspended VMs).
            var statusResource = isContainer
                ? $"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/status/current"
                : $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/status/current";

            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            var lastMatched = false;

            return Invoke(session, client =>
            {
                while (DateTime.UtcNow < deadline)
                {
                    try
                    {
                        var json = client.GetAsync(statusResource).GetAwaiter().GetResult();
                        var snapshot = GuestStatusSnapshot.Evaluate(json, expectedStatus);
                        lastMatched = snapshot.StatusMatched;

                        // snapshot.Locked is the config `lock:` property (backup, clone, migrate,
                        // snapshot) — not the /var/lock/qemu-server flock, which PVE does not
                        // expose. The flock race is handled by retrying, not by waiting.
                        if (snapshot.StatusMatched && !snapshot.Locked)
                            return task;
                    }
                    catch (PveApiException ex) when (
                        ex.StatusCode != HttpStatusCode.Unauthorized
                        && ex.StatusCode != HttpStatusCode.Forbidden
                        && ex.StatusCode != HttpStatusCode.NotFound)
                    {
                        onProgress?.Invoke($"Status poll failed, retrying: {ex.Message}");
                    }

                    _pollDelay(PollInterval).GetAwaiter().GetResult();
                }

                // The guest still reports the expected status on the final poll and only the
                // lock outlasted the deadline.
                if (lastMatched)
                    return task;

                throw new PveTaskTimeoutException(
                    task.Upid ?? "unknown",
                    TimeSpan.FromSeconds(timeoutSeconds));
            });
        }
    }
}
