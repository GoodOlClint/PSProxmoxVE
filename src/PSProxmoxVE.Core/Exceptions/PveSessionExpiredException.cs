using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when the Proxmox VE session ticket has expired.</summary>
    public class PveSessionExpiredException : Exception
    {
        /// <summary>Initializes a new instance indicating the session has expired.</summary>
        public PveSessionExpiredException()
            : base("Your Proxmox VE session has expired. Please run Connect-PveServer to establish a new session.")
        {
        }

        /// <summary>Initializes a new instance indicating the session has expired, with an inner exception.</summary>
        /// <param name="innerException">The exception that caused this failure.</param>
        public PveSessionExpiredException(Exception innerException)
            : base("Your Proxmox VE session has expired. Please run Connect-PveServer to establish a new session.", innerException)
        {
        }
    }
}
