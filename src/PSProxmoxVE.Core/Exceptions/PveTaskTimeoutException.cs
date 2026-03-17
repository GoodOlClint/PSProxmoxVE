using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when a Proxmox VE task does not complete within the allowed timeout period</summary>
    public class PveTaskTimeoutException : Exception
    {
        public string Upid { get; }
        public TimeSpan Timeout { get; }

        public PveTaskTimeoutException(string upid, TimeSpan timeout)
            : base($"Task {upid} did not complete within {timeout.TotalSeconds} seconds.")
        {
            Upid = upid;
            Timeout = timeout;
        }

        public PveTaskTimeoutException(string upid, TimeSpan timeout, Exception innerException)
            : base($"Task {upid} did not complete within {timeout.TotalSeconds} seconds.", innerException)
        {
            Upid = upid;
            Timeout = timeout;
        }
    }
}
