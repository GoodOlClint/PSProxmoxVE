using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when authentication to a Proxmox VE server fails.</summary>
    public class PveAuthenticationException : Exception
    {
        /// <summary>Initializes a new instance with the specified error message.</summary>
        /// <param name="message">The error message describing the authentication failure.</param>
        public PveAuthenticationException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance with the specified error message and inner exception.</summary>
        /// <param name="message">The error message describing the authentication failure.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public PveAuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
