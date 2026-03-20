using System;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when the connected Proxmox VE server does not meet the minimum version requirement for an operation.</summary>
    public class PveVersionException : Exception
    {
        /// <summary>The minimum required major version number.</summary>
        public int RequiredMajor { get; }

        /// <summary>The minimum required minor version number.</summary>
        public int RequiredMinor { get; }

        /// <summary>The actual server version that did not meet the requirement.</summary>
        public PveVersion ActualVersion { get; }

        /// <summary>Initializes a new instance for a version requirement that was not met.</summary>
        /// <param name="requiredMajor">The minimum required major version.</param>
        /// <param name="requiredMinor">The minimum required minor version.</param>
        /// <param name="actualVersion">The actual server version detected.</param>
        public PveVersionException(int requiredMajor, int requiredMinor, PveVersion actualVersion)
            : base($"This operation requires Proxmox VE {requiredMajor}.{requiredMinor} or later. Connected server is version {actualVersion}.")
        {
            RequiredMajor = requiredMajor;
            RequiredMinor = requiredMinor;
            ActualVersion = actualVersion;
        }

        /// <summary>Initializes a new instance for a version requirement that was not met, with an inner exception.</summary>
        /// <param name="requiredMajor">The minimum required major version.</param>
        /// <param name="requiredMinor">The minimum required minor version.</param>
        /// <param name="actualVersion">The actual server version detected.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public PveVersionException(int requiredMajor, int requiredMinor, PveVersion actualVersion, Exception innerException)
            : base($"This operation requires Proxmox VE {requiredMajor}.{requiredMinor} or later. Connected server is version {actualVersion}.", innerException)
        {
            RequiredMajor = requiredMajor;
            RequiredMinor = requiredMinor;
            ActualVersion = actualVersion;
        }
    }
}
