using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when a Proxmox VE task does not complete within the allowed timeout period.</summary>
    public class PveTaskTimeoutException : Exception
    {
        /// <summary>The UPID of the task that timed out.</summary>
        public string Upid { get; }

        /// <summary>The timeout duration that was exceeded.</summary>
        public TimeSpan Timeout { get; }

        /// <summary>Initializes a new instance for a task that exceeded the specified timeout.</summary>
        /// <param name="upid">The UPID of the timed-out task.</param>
        /// <param name="timeout">The timeout duration that was exceeded.</param>
        public PveTaskTimeoutException(string upid, TimeSpan timeout)
            : base($"Task {upid} did not complete within {timeout.TotalSeconds} seconds.")
        {
            Upid = upid;
            Timeout = timeout;
        }

        /// <summary>Initializes a new instance for a task that exceeded the specified timeout, with an inner exception.</summary>
        /// <param name="upid">The UPID of the timed-out task.</param>
        /// <param name="timeout">The timeout duration that was exceeded.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public PveTaskTimeoutException(string upid, TimeSpan timeout, Exception innerException)
            : base($"Task {upid} did not complete within {timeout.TotalSeconds} seconds.", innerException)
        {
            Upid = upid;
            Timeout = timeout;
        }
    }
}
