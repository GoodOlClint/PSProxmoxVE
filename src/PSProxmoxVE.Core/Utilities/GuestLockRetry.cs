using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PSProxmoxVE.Core.Exceptions;

namespace PSProxmoxVE.Core.Utilities
{
    /// <summary>
    /// Retries an operation PVE rejected because it could not acquire a guest's config
    /// flock (<c>/var/lock/qemu-server/lock-&lt;vmid&gt;.conf</c> for VMs,
    /// <c>/run/lock/lxc/pve-config-&lt;vmid&gt;.lock</c> for containers).
    ///
    /// That flock is held by <c>qm cleanup</c> after a guest stops and is not exposed
    /// through the API in any form, so a caller cannot wait for it — only retry past it.
    /// </summary>
    public static class GuestLockRetry
    {
        /// <summary>
        /// How long <see cref="Execute{T}"/> and <see cref="ExecuteAsync{T}"/> keep retrying.
        /// <c>qm cleanup</c> polls <c>vm_running_locally</c> for up to 30s while holding the
        /// flock, and each rejected attempt first burns PVE's own 10s <c>lock_config</c> timeout.
        /// </summary>
        public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(45);

        private static readonly TimeSpan MaxRetryInterval = TimeSpan.FromSeconds(2);

        // The gap must never eat a meaningful share of a short budget: a caller passing a
        // small window wants a fast answer, not one long sleep.
        private static TimeSpan RetryInterval(TimeSpan budget)
        {
            var quarter = TimeSpan.FromMilliseconds(budget.TotalMilliseconds / 4);
            return quarter < MaxRetryInterval ? quarter : MaxRetryInterval;
        }

        // Anchored, and specific to the two guest lock paths. `PVE::Tools::lock_file` emits this
        // same wording for storage, LVM, HA and firewall locks, none of which carry the
        // reissue-safety guarantee below. The anchor is what separates a failure to *enter*
        // lock_config from one PVE prefixed with its own context ("clone failed: ..."), which
        // means the worker had already done work.
        private static readonly Regex GuestLockTimeout = new Regex(
            @"^can't lock file '(?:/var/lock/qemu-server/lock-\d+\.conf|/run/lock/lxc/pve-config-\d+\.lock)' - got timeout",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// True when <paramref name="ex"/> reports PVE failing to enter <c>lock_config</c> for a
        /// guest. That is raised before the operation performs any work, so a call failing this
        /// way is known not to have run and is safe to reissue.
        ///
        /// Matched against what PVE actually said — <see cref="PveTaskFailedException.ExitStatus"/>
        /// and <see cref="PveApiException.ApiMessage"/> — never against the composed
        /// <see cref="Exception.Message"/>, whose prefix would defeat the anchor.
        /// </summary>
        /// <param name="ex">The exception to classify.</param>
        public static bool IsLockTimeout(Exception ex) => ex switch
        {
            PveTaskFailedException task => GuestLockTimeout.IsMatch((task.ExitStatus ?? string.Empty).Trim()),
            PveApiException api => GuestLockTimeout.IsMatch((api.ApiMessage ?? string.Empty).Trim()),
            _ => false,
        };

        /// <summary>
        /// Runs <paramref name="operation"/>, reissuing it while it fails on the guest config
        /// flock and <paramref name="window"/> has not elapsed. Any other exception propagates
        /// on the first attempt.
        /// </summary>
        /// <param name="operation">The operation to run.</param>
        /// <param name="window">Retry budget. Defaults to <see cref="DefaultWindow"/>.</param>
        /// <param name="onRetry">
        ///   Invoked with the rejection before each reissue. A caller with somewhere to report
        ///   progress should pass one — a wait this long is otherwise indistinguishable from a hang.
        /// </param>
        public static T Execute<T>(Func<T> operation, TimeSpan? window = null, Action<Exception>? onRetry = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            var budget = window ?? DefaultWindow;
            var elapsed = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex) when (IsLockTimeout(ex) && elapsed.Elapsed < budget)
                {
                    onRetry?.Invoke(ex);
                    Thread.Sleep(RetryInterval(budget));
                }
            }
        }

        /// <summary>Asynchronous counterpart of <see cref="Execute{T}"/>.</summary>
        /// <param name="operation">The operation to run.</param>
        /// <param name="window">Retry budget. Defaults to <see cref="DefaultWindow"/>.</param>
        public static Task<T> ExecuteAsync<T>(Func<Task<T>> operation, TimeSpan? window = null) =>
            ExecuteAsync(operation, window, Task.Delay);

        /// <summary>
        /// Test seam: same as <see cref="ExecuteAsync{T}(Func{Task{T}}, TimeSpan?)"/> but with the
        /// inter-attempt wait replaceable, so a test can assert retry counts without paying the
        /// wall-clock cost of <see cref="RetryInterval"/>.
        /// </summary>
        /// <param name="operation">The operation to run.</param>
        /// <param name="window">Retry budget. Defaults to <see cref="DefaultWindow"/>.</param>
        /// <param name="delay">Invoked with the computed retry interval before each reissue.</param>
        internal static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, TimeSpan? window, Func<TimeSpan, Task> delay)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (delay == null) throw new ArgumentNullException(nameof(delay));

            var budget = window ?? DefaultWindow;
            var elapsed = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex) when (IsLockTimeout(ex) && elapsed.Elapsed < budget)
                {
                    await delay(RetryInterval(budget)).ConfigureAwait(false);
                }
            }
        }
    }
}
