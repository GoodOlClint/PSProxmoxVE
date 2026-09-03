using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for querying and waiting on Proxmox VE asynchronous tasks (UPIDs).
    /// </summary>
    public class TaskService : PveServiceBase
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan MinPollInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxBackoffInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan BackoffStep = TimeSpan.FromSeconds(1);

        private readonly Func<TimeSpan, Task> _pollDelay;

        /// <summary>Initializes a new instance that creates its own HTTP clients.</summary>
        public TaskService() : this(Sleep) { }

        /// <summary>Initializes a new instance that uses the supplied HTTP client for all requests.</summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        public TaskService(IPveHttpClient client) : this(client, Sleep) { }

        /// <summary>
        /// Test seam: same as <see cref="TaskService()"/> but with the wait between polls
        /// replaceable, so a test can assert the poll schedule without sleeping for it.
        /// </summary>
        /// <param name="pollDelay">Invoked with each computed poll interval instead of sleeping.</param>
        internal TaskService(Func<TimeSpan, Task> pollDelay)
        {
            _pollDelay = pollDelay ?? throw new ArgumentNullException(nameof(pollDelay));
        }

        /// <summary>
        /// Test seam: same as <see cref="TaskService(IPveHttpClient)"/> but with the wait between
        /// polls replaceable, so a test can assert the poll schedule without sleeping for it.
        /// </summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        /// <param name="pollDelay">Invoked with each computed poll interval instead of sleeping.</param>
        internal TaskService(IPveHttpClient client, Func<TimeSpan, Task> pollDelay) : base(client)
        {
            _pollDelay = pollDelay ?? throw new ArgumentNullException(nameof(pollDelay));
        }

        private static Task Sleep(TimeSpan duration)
        {
            Thread.Sleep(duration);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Returns the current status of a task identified by its UPID.
        /// </summary>
        public PveTask GetTask(PveSession session, string node, string upid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(upid)) throw new ArgumentNullException(nameof(upid));

            return Invoke(session, client =>
            {
                var encodedUpid = Uri.EscapeDataString(upid);
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/tasks/{encodedUpid}/status")
                    .GetAwaiter().GetResult();
                return ParseTaskStatus(response, node, upid);
            });
        }

        private static PveTask ParseTaskStatus(string response, string node, string upid)
        {
            var data = JObject.Parse(response)["data"];
            var task = data?.ToObject<PveTask>() ?? new PveTask { Upid = upid };
            task.Node = node;
            return task;
        }

        /// <summary>
        /// Polls the task status until it completes, throws on timeout or failure. One HTTP
        /// client is held open for the whole wait.
        /// </summary>
        /// <param name="session">Active PVE session.</param>
        /// <param name="node">Node name where the task is running.</param>
        /// <param name="upid">Task UPID.</param>
        /// <param name="timeout">Maximum time to wait. Defaults to 10 minutes.</param>
        /// <param name="pollInterval">
        ///   Fixed interval between status polls, minimum 1 second. When omitted the interval
        ///   starts at 1 second and grows by 1 second per poll up to a 10 second cap. A wait
        ///   never sleeps past <paramref name="timeout"/>.
        /// </param>
        /// <param name="progressCallback">Optional callback invoked on each poll with the current task.</param>
        /// <returns>The completed <see cref="PveTask"/>.</returns>
        /// <exception cref="PveTaskTimeoutException">Thrown when the task does not complete within <paramref name="timeout"/>.</exception>
        /// <exception cref="PveTaskFailedException">Thrown when the task completes with a non-OK exit status.</exception>
        public PveTask WaitForTask(
            PveSession session,
            string node,
            string upid,
            TimeSpan? timeout = null,
            TimeSpan? pollInterval = null,
            Action<PveTask>? progressCallback = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(upid)) throw new ArgumentNullException(nameof(upid));

            var effectiveTimeout = timeout ?? DefaultTimeout;
            var fixedInterval = pollInterval.HasValue
                ? (pollInterval.Value < MinPollInterval ? MinPollInterval : pollInterval.Value)
                : (TimeSpan?)null;

            var deadline = DateTime.UtcNow.Add(effectiveTimeout);
            var statusResource = $"nodes/{Uri.EscapeDataString(node)}/tasks/{Uri.EscapeDataString(upid)}/status";

            return Invoke(session, client =>
            {
                var interval = fixedInterval ?? MinPollInterval;
                while (true)
                {
                    var response = client.GetAsync(statusResource).GetAwaiter().GetResult();
                    var task = ParseTaskStatus(response, node, upid);
                    progressCallback?.Invoke(task);

                    if (string.Equals(task.Status, "stopped", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.Equals(task.ExitStatus, "OK", StringComparison.OrdinalIgnoreCase))
                            throw new PveTaskFailedException(upid, task.ExitStatus ?? "(no exit status)");
                        return task;
                    }

                    var now = DateTime.UtcNow;
                    if (now >= deadline)
                        throw new PveTaskTimeoutException(upid, effectiveTimeout);

                    var remaining = deadline - now;
                    _pollDelay(interval < remaining ? interval : remaining).GetAwaiter().GetResult();

                    if (!fixedInterval.HasValue && interval < MaxBackoffInterval)
                    {
                        interval += BackoffStep;
                        if (interval > MaxBackoffInterval)
                            interval = MaxBackoffInterval;
                    }
                }
            });
        }

        /// <summary>
        /// Returns a list of tasks on the specified node, with optional filters.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The node name.</param>
        /// <param name="vmid">Optional VM ID filter.</param>
        /// <param name="source">Optional source filter: "all" or "active".</param>
        /// <param name="typeFilter">Optional task type filter (e.g., "qmstart").</param>
        /// <param name="limit">Maximum number of tasks to return. Defaults to 50.</param>
        public PveTask[] GetTasks(PveSession session, string node, int? vmid = null,
            string? source = null, string? typeFilter = null, int limit = 50)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var queryParts = new List<string> { $"limit={limit}" };
                if (vmid.HasValue)
                    queryParts.Add($"vmid={vmid.Value}");
                if (!string.IsNullOrEmpty(source))
                    queryParts.Add($"source={Uri.EscapeDataString(source!)}");
                if (!string.IsNullOrEmpty(typeFilter))
                    queryParts.Add($"typefilter={Uri.EscapeDataString(typeFilter!)}");

                var query = string.Join("&", queryParts);
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/tasks?{query}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var tasks = data?.ToObject<PveTask[]>() ?? Array.Empty<PveTask>();
                foreach (var t in tasks)
                    t.Node ??= node;
                return tasks;
            });
        }

        /// <summary>
        /// Stops (cancels) a running task on the specified node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The node name.</param>
        /// <param name="upid">The UPID of the task to stop.</param>
        public void StopTask(PveSession session, string node, string upid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(upid)) throw new ArgumentNullException(nameof(upid));

            Invoke(session, client =>
            {
                var encodedUpid = Uri.EscapeDataString(upid);
                client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/tasks/{encodedUpid}")
                    .GetAwaiter().GetResult();
            });
        }
    }
}
