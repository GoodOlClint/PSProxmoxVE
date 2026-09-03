using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Errors;
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
        /// Runs <see cref="ProcessPveRecord"/> and reports any module exception that escapes it
        /// as a mapped terminating error, so a 403, a 404 and an unreachable server are
        /// distinguishable by <c>$_.CategoryInfo</c> and <c>$_.FullyQualifiedErrorId</c>.
        /// </summary>
        protected sealed override void ProcessRecord()
        {
            try
            {
                ProcessPveRecord();
            }
            catch (Exception ex) when (PveErrorMapper.IsRecognized(ex))
            {
                ThrowTerminatingError(ToPveErrorRecord(ex, null));
            }
        }

        /// <summary>The cmdlet's per-record work. Overrides <see cref="ProcessRecord"/>'s body.</summary>
        protected abstract void ProcessPveRecord();

        /// <summary>
        /// Builds the <see cref="ErrorRecord"/> for <paramref name="exception"/>, classifying it
        /// with <see cref="PveErrorMapper"/>.
        /// </summary>
        /// <param name="exception">The failure to report.</param>
        /// <param name="target">
        /// The object the failure is about. When null, the target is derived from the exception
        /// (the API resource, or the task UPID).
        /// </param>
        /// <returns>The error record to write or throw.</returns>
        protected ErrorRecord ToPveErrorRecord(Exception exception, object? target)
        {
            var descriptor = PveErrorMapper.Describe(exception);
            return new ErrorRecord(
                exception,
                descriptor.ErrorId,
                ToErrorCategory(descriptor.Kind),
                target ?? descriptor.Target);
        }

        private static ErrorCategory ToErrorCategory(PveErrorKind kind) => kind switch
        {
            PveErrorKind.PermissionDenied => ErrorCategory.PermissionDenied,
            PveErrorKind.AuthenticationError => ErrorCategory.AuthenticationError,
            PveErrorKind.ObjectNotFound => ErrorCategory.ObjectNotFound,
            PveErrorKind.InvalidArgument => ErrorCategory.InvalidArgument,
            PveErrorKind.OperationTimeout => ErrorCategory.OperationTimeout,
            PveErrorKind.ConnectionError => ErrorCategory.ConnectionError,
            PveErrorKind.ResourceUnavailable => ErrorCategory.ResourceUnavailable,
            PveErrorKind.InvalidOperation => ErrorCategory.InvalidOperation,
            PveErrorKind.OperationStopped => ErrorCategory.OperationStopped,
            _ => ErrorCategory.NotSpecified,
        };

        /// <summary>
        /// Returns the session to use for this cmdlet.
        /// Resolution order: -Session parameter → the runspace's module session.
        /// Throws <see cref="PveNotConnectedException"/> if no session is available,
        /// or <see cref="PveSessionExpiredException"/> if the session ticket has expired.
        /// </summary>
        protected PveSession GetSession()
        {
            var session = Session ?? ModuleState.GetActiveSession(this);

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
                ThrowTerminatingError(ToPveErrorRecord(
                    new PveVersionException(introducedMajor, introducedMinor, version),
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
        /// Emits a soft warning when <paramref name="condition"/> holds and the connected
        /// server is below <paramref name="requiredMajor"/>.<paramref name="requiredMinor"/>.
        /// Unlike <see cref="RequireVersion"/>, this never blocks the call: the parameter or
        /// feature may simply be silently ignored by an older server.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="condition">True when the caller used the parameter/feature this warning covers.</param>
        /// <param name="requiredMajor">Major version the parameter/feature requires.</param>
        /// <param name="requiredMinor">Minor version the parameter/feature requires.</param>
        /// <param name="requirementClause">
        /// The warning's leading clause, ending in "requires"/"require" (e.g. "The -DhcpRange
        /// parameter requires"), so the full sentence reads
        /// "&lt;requirementClause&gt; PVE &lt;requiredMajor&gt;.&lt;requiredMinor&gt; or later.".
        /// </param>
        /// <param name="consequence">Trailing sentence describing what happens if the server is too old.</param>
        protected void WarnIfBelowVersion(
            PveSession session,
            bool condition,
            int requiredMajor,
            int requiredMinor,
            string requirementClause,
            string consequence)
        {
            if (!condition) return;

            var version = session.ServerVersion;
            if (version == null || version.IsAtLeast(requiredMajor, requiredMinor)) return;

            WriteWarning(
                $"{requirementClause} PVE {requiredMajor}.{requiredMinor} or later. " +
                $"Connected server is PVE {version}. {consequence}");
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
            return new GuestLifecycleService().WaitForStatusTransition(
                session, node, issueOperation, vmid, expectedStatus, timeoutSeconds, isContainer,
                onProgress: WriteVerbose);
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
            return new GuestLifecycleService().InvokeGuestTask(session, node, issueOperation, onProgress: WriteVerbose);
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
            var (result, malformed) = CorosyncLinks.Parse(links);
            foreach (var link in malformed)
                WriteWarning($"Ignoring malformed link entry '{link}'. Expected format: 'link0=10.0.0.1'");
            return result;
        }
    }
}
