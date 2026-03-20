using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when a Proxmox VE task completes with a failed exit status.</summary>
    public class PveTaskFailedException : Exception
    {
        /// <summary>The UPID of the failed task.</summary>
        public string Upid { get; }

        /// <summary>The exit status string reported by the task (e.g., an error message).</summary>
        public string ExitStatus { get; }

        /// <summary>Initializes a new instance for a task that failed with the specified exit status.</summary>
        /// <param name="upid">The UPID of the failed task.</param>
        /// <param name="exitStatus">The exit status string reported by the task.</param>
        public PveTaskFailedException(string upid, string exitStatus)
            : base($"Task {upid} failed with exit status: {exitStatus}")
        {
            Upid = upid;
            ExitStatus = exitStatus;
        }

        /// <summary>Initializes a new instance for a task that failed, with an inner exception.</summary>
        /// <param name="upid">The UPID of the failed task.</param>
        /// <param name="exitStatus">The exit status string reported by the task.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public PveTaskFailedException(string upid, string exitStatus, Exception innerException)
            : base($"Task {upid} failed with exit status: {exitStatus}", innerException)
        {
            Upid = upid;
            ExitStatus = exitStatus;
        }
    }
}
