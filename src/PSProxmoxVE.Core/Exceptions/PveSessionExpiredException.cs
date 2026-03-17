using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when the Proxmox VE session ticket has expired</summary>
    public class PveSessionExpiredException : Exception
    {
        public PveSessionExpiredException()
            : base("Your Proxmox VE session has expired. Please run Connect-PveServer to establish a new session.")
        {
        }

        public PveSessionExpiredException(Exception innerException)
            : base("Your Proxmox VE session has expired. Please run Connect-PveServer to establish a new session.", innerException)
        {
        }
    }
}
