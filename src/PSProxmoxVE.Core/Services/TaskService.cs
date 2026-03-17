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
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MinPollInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Returns the current status of a task identified by its UPID.
        /// </summary>
        public PveTask GetTask(PveSession session, string node, string upid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(upid)) throw new ArgumentNullException(nameof(upid));

            using var client = new PveHttpClient(session);
            var encodedUpid = Uri.EscapeDataString(upid);
            var response = client.GetAsync($"nodes/{node}/tasks/{encodedUpid}/status")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            var task = data?.ToObject<PveTask>() ?? new PveTask { Upid = upid };
            task.Node = node;
            return task;
        }

        /// <summary>
        /// Returns all log lines for a task identified by its UPID.
        /// </summary>
        public PveTaskLog[] GetTaskLog(PveSession session, string node, string upid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(upid)) throw new ArgumentNullException(nameof(upid));

            using var client = new PveHttpClient(session);
            var encodedUpid = Uri.EscapeDataString(upid);
            var response = client.GetAsync($"nodes/{node}/tasks/{encodedUpid}/log")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveTaskLog[]>() ?? Array.Empty<PveTaskLog>();
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
    }
}
