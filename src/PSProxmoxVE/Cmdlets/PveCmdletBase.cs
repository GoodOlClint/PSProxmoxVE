using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Cmdlets
{
    /// <summary>
    /// Base class for all PSProxmoxVE cmdlets. Provides optional -Session parameter
    /// and a helper method to resolve and validate the active session.
    /// </summary>
    public abstract class PveCmdletBase : PSCmdlet
    {
        /// <summary>
        /// An explicit PveSession to use for this cmdlet invocation.
        /// When omitted, the module-level session stored by Connect-PveServer is used.
        /// </summary>
        [Parameter(Mandatory = false)]
        public PveSession? Session { get; set; }

        /// <summary>
        /// Returns the session to use for this cmdlet.
        /// Resolution order: -Session parameter → ModuleState.ActiveSession.
        /// Throws <see cref="PveNotConnectedException"/> if no session is available,
        /// or <see cref="PveSessionExpiredException"/> if the session ticket has expired.
        /// </summary>
        protected PveSession GetSession()
        {
            var session = Session ?? ModuleState.ActiveSession;

            if (session is null)
                throw new PveNotConnectedException();

            if (session.IsExpired)
                throw new PveSessionExpiredException();

            return session;
        }

        /// <summary>
        /// Checks the connected PVE server version against a two-tier requirement:
        /// <list type="bullet">
        /// <item><b>Introduced</b> — the API endpoint was added in this version.
        ///   If the server is older, the cmdlet emits a terminating error because
        ///   the endpoint does not exist at all.</item>
        /// <item><b>Default</b> (optional) — the feature is installed/enabled by
        ///   default since this version. If the server is between <paramref name="introducedMajor"/>
        ///   and <paramref name="defaultMajor"/>, a warning is emitted but the call
        ///   proceeds, allowing users who manually enabled the feature to succeed.</item>
        /// </list>
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="featureName">Human-readable name shown in messages (e.g. "SDN IPAM").</param>
        /// <param name="introducedMajor">Major version that introduced the API endpoint.</param>
        /// <param name="introducedMinor">Minor version that introduced the API endpoint.</param>
        /// <param name="defaultMajor">Major version where the feature is enabled by default (null to skip warning tier).</param>
        /// <param name="defaultMinor">Minor version where the feature is enabled by default.</param>
        protected void RequireVersion(
            PveSession session,
            string featureName,
            int introducedMajor,
            int introducedMinor,
            int? defaultMajor = null,
            int? defaultMinor = null)
        {
            var version = session.ServerVersion;
            if (version == null) return; // version unknown — optimistic, let the call proceed

            // Hard fail: endpoint does not exist
            if (!version.IsAtLeast(introducedMajor, introducedMinor))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new PveVersionException(introducedMajor, introducedMinor, version),
                    "PveVersionTooOld",
                    ErrorCategory.InvalidOperation,
                    null));
                return;
            }

            // Soft warning: feature exists but may not be enabled by default
            if (defaultMajor.HasValue && defaultMinor.HasValue
                && !version.IsAtLeast(defaultMajor.Value, defaultMinor.Value))
            {
                WriteWarning(
                    $"{featureName} is available since PVE {introducedMajor}.{introducedMinor} but is not enabled by default until PVE {defaultMajor}.{defaultMinor}. " +
                    $"Connected server is PVE {version}. The command will proceed, but may fail if the feature is not manually enabled.");
            }
        }

        /// <summary>
        /// Waits for a PVE task to complete, then optionally polls VM status until
        /// it matches <paramref name="expectedStatus"/>. Used by lifecycle cmdlets
        /// (Start, Stop, Suspend, Resume, etc.) when -Wait is specified.
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
        /// <returns>The completed task.</returns>
        protected PveTask WaitForStatusTransition(
            PveSession session,
            string node,
            Func<PveTask> issueOperation,
            int vmid,
            string expectedStatus,
            int timeoutSeconds = 60,
            bool isContainer = false)
        {
            var task = InvokeGuestTask(session, node, issueOperation);

            // Then poll status/current until VM/container reaches the expected status.
            // We query the status/current endpoint directly instead of the list endpoint
            // because it returns qmpstatus (needed for paused state detection — PVE reports
            // status=running but qmpstatus=paused for suspended VMs).
            var statusResource = isContainer
                ? $"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/status/current"
                : $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/status/current";

            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            var lastMatched = false;
            using var pollClient = new PveHttpClient(session);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var json = pollClient.GetAsync(statusResource).GetAwaiter().GetResult();
                    var snapshot = GuestStatusSnapshot.Evaluate(json, expectedStatus);
                    lastMatched = snapshot.StatusMatched;

                    // snapshot.Locked is the config `lock:` property (backup, clone, migrate,
                    // snapshot) — not the /var/lock/qemu-server flock, which PVE does not
                    // expose. The flock race is handled by retrying, not by waiting.
                    if (snapshot.StatusMatched && !snapshot.Locked)
                        return task;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    // Ignore transient errors during polling
                }

                System.Threading.Thread.Sleep(2000);
            }

            // The guest still reports the expected status on the final poll and only the
            // lock outlasted the deadline.
            if (lastMatched)
                return task;

            throw new PveTaskTimeoutException(
                task.Upid ?? "unknown",
                TimeSpan.FromSeconds(timeoutSeconds));
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
        /// <returns>The completed task, or the issued task when the call returned no UPID.</returns>
        protected PveTask InvokeGuestTask(PveSession session, string node, Func<PveTask> issueOperation)
        {
            if (issueOperation == null) throw new ArgumentNullException(nameof(issueOperation));

            var taskService = new TaskService();
            return GuestLockRetry.Execute(() =>
            {
                var task = issueOperation();
                return string.IsNullOrEmpty(task.Upid)
                    ? task
                    : taskService.WaitForTask(session, node, task.Upid, null, null, null);
            });
        }

        /// <summary>
        /// Extracts the node name from a UPID string (format: UPID:node:...).
        /// Falls back to <paramref name="fallback"/> if the UPID is empty or cannot be parsed.
        /// </summary>
        protected static string GetNodeFromUpid(string? upid, string fallback)
        {
            if (upid != null && upid.Length > 0)
            {
                var parts = upid.Split(':');
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                    return parts[1];
            }
            return fallback;
        }

        /// <summary>
        /// Parses an array of Corosync link strings (e.g. "link0=10.0.0.1") into a dictionary.
        /// Emits a warning for entries that do not match the expected "key=value" format.
        /// </summary>
        /// <param name="links">Array of link strings in "linkN=address" format.</param>
        /// <returns>Dictionary of parsed link entries, or null if input is null.</returns>
        protected Dictionary<string, string>? ParseLinks(string[]? links)
        {
            if (links == null) return null;

            var result = new Dictionary<string, string>();
            foreach (var link in links)
            {
                var parts = link.Split(new[] { '=' }, 2);
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                {
                    WriteWarning($"Ignoring malformed link entry '{link}'. Expected format: 'link0=10.0.0.1'");
                    continue;
                }
                result[parts[0].Trim()] = parts[1].Trim();
            }
            return result.Count > 0 ? result : null;
        }
    }
}
