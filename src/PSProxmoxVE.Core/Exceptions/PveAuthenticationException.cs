using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when authentication to a Proxmox VE server fails</summary>
    public class PveAuthenticationException : Exception
    {
        public PveAuthenticationException(string message)
            : base(message)
        {
        }

        public PveAuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
