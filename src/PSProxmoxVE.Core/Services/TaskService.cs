using System;
using System.Collections.Generic;
using System.Threading;
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
    public class TaskService
    {
        private readonly IPveHttpClient? _injectedClient;

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MinPollInterval = TimeSpan.FromSeconds(1);

        /// <summary>Initializes a new instance that creates its own HTTP clients.</summary>
        public TaskService() { }

        /// <summary>Initializes a new instance that uses the supplied HTTP client for all requests.</summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        public TaskService(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Returns the current status of a task identified by its UPID.
        /// </summary>
        public PveTask GetTask(PveSession session, string node, string upid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(upid)) throw new ArgumentNullException(nameof(upid));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var encodedUpid = Uri.EscapeDataString(upid);
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/tasks/{encodedUpid}/status")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var task = data?.ToObject<PveTask>() ?? new PveTask { Upid = upid };
                task.Node = node;
                return task;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns all log lines for a task identified by its UPID.
        /// </summary>
        public PveTaskLog[] GetTaskLog(PveSession session, string node, string upid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(upid)) throw new ArgumentNullException(nameof(upid));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var encodedUpid = Uri.EscapeDataString(upid);
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/tasks/{encodedUpid}/log")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveTaskLog[]>() ?? Array.Empty<PveTaskLog>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Polls the task status until it completes, throws on timeout or failure.
        /// </summary>
        /// <param name="session">Active PVE session.</param>
        /// <param name="node">Node name where the task is running.</param>
        /// <param name="upid">Task UPID.</param>
        /// <param name="timeout">Maximum time to wait. Defaults to 10 minutes.</param>
        /// <param name="pollInterval">Interval between status polls. Defaults to 2 seconds. Minimum 1 second.</param>
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
            var effectivePoll = pollInterval ?? DefaultPollInterval;
            if (effectivePoll < MinPollInterval)
                effectivePoll = MinPollInterval;

            var deadline = DateTime.UtcNow.Add(effectiveTimeout);

            while (true)
            {
                var task = GetTask(session, node, upid);
                progressCallback?.Invoke(task);

                if (string.Equals(task.Status, "stopped", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(task.ExitStatus, "OK", StringComparison.OrdinalIgnoreCase))
                        throw new PveTaskFailedException(upid, task.ExitStatus ?? "(no exit status)");
                    return task;
                }

                if (DateTime.UtcNow >= deadline)
                    throw new PveTaskTimeoutException(upid, effectiveTimeout);

                Thread.Sleep(effectivePoll);
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
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
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var encodedUpid = Uri.EscapeDataString(upid);
                client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/tasks/{encodedUpid}")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }
    }
}
