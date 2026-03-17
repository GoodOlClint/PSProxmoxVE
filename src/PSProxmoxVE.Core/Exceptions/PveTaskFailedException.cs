using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when a Proxmox VE task completes with a failed exit status</summary>
    public class PveTaskFailedException : Exception
    {
        public string Upid { get; }
        public string ExitStatus { get; }

        public PveTaskFailedException(string upid, string exitStatus)
            : base($"Task {upid} failed with exit status: {exitStatus}")
        {
            Upid = upid;
            ExitStatus = exitStatus;
        }

        public PveTaskFailedException(string upid, string exitStatus, Exception innerException)
            : base($"Task {upid} failed with exit status: {exitStatus}", innerException)
        {
            Upid = upid;
            ExitStatus = exitStatus;
        }
    }
}
