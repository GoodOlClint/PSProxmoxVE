using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when the Proxmox VE session ticket has expired.</summary>
    public class PveSessionExpiredException : Exception
    {
        private const string DefaultMessage =
            "Your Proxmox VE session has expired. Please run Connect-PveServer to establish a new session.";

        /// <summary>Initializes a new instance indicating the session has expired.</summary>
        public PveSessionExpiredException()
            : base(DefaultMessage)
        {
        }

        /// <summary>Initializes a new instance indicating the session has expired, with an inner exception.</summary>
        /// <param name="innerException">The exception that caused this failure.</param>
        public PveSessionExpiredException(Exception innerException)
            : base(DefaultMessage, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance indicating the session has expired, appending
        /// <paramref name="detail"/> to the message, with an inner exception.
        /// </summary>
        /// <param name="detail">What was attempted to keep the session alive, and how it failed.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public PveSessionExpiredException(string detail, Exception innerException)
            : base(DefaultMessage + " " + detail, innerException)
        {
        }
    }
}
