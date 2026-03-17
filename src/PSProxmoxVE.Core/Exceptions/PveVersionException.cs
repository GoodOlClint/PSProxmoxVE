using System;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when the connected Proxmox VE server does not meet the minimum version requirement for an operation</summary>
    public class PveVersionException : Exception
    {
        public int RequiredMajor { get; }
        public int RequiredMinor { get; }
        public PveVersion ActualVersion { get; }

        public PveVersionException(int requiredMajor, int requiredMinor, PveVersion actualVersion)
            : base($"This operation requires Proxmox VE {requiredMajor}.{requiredMinor} or later. Connected server is version {actualVersion}.")
        {
            RequiredMajor = requiredMajor;
            RequiredMinor = requiredMinor;
            ActualVersion = actualVersion;
        }

        public PveVersionException(int requiredMajor, int requiredMinor, PveVersion actualVersion, Exception innerException)
            : base($"This operation requires Proxmox VE {requiredMajor}.{requiredMinor} or later. Connected server is version {actualVersion}.", innerException)
        {
            RequiredMajor = requiredMajor;
            RequiredMinor = requiredMinor;
            ActualVersion = actualVersion;
        }
    }
}
