using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when an operation is attempted without an active Proxmox VE session.</summary>
    public class PveNotConnectedException : Exception
    {
        /// <summary>Initializes a new instance indicating no active session exists.</summary>
        public PveNotConnectedException()
            : base("No active Proxmox VE session. Run Connect-PveServer first.")
        {
        }

        /// <summary>Initializes a new instance indicating no active session exists, with an inner exception.</summary>
        /// <param name="innerException">The exception that caused this failure.</param>
        public PveNotConnectedException(Exception innerException)
            : base("No active Proxmox VE session. Run Connect-PveServer first.", innerException)
        {
        }
    }
}
