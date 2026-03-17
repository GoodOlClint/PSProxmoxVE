using System;

namespace PSProxmoxVE.Core.Exceptions
{
    /// <summary>Exception thrown when an operation is attempted without an active Proxmox VE session</summary>
    public class PveNotConnectedException : Exception
    {
        public PveNotConnectedException()
            : base("No active Proxmox VE session. Run Connect-PveServer first.")
        {
        }

        public PveNotConnectedException(Exception innerException)
            : base("No active Proxmox VE session. Run Connect-PveServer first.", innerException)
        {
        }
    }
}
