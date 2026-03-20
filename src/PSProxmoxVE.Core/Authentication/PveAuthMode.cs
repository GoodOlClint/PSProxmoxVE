namespace PSProxmoxVE.Core.Authentication
{
    /// <summary>Authentication mode for Proxmox VE API.</summary>
    public enum PveAuthMode
    {
        /// <summary>Authenticate using a username/password ticket and CSRF token.</summary>
        Ticket,

        /// <summary>Authenticate using a persistent API token (USER@REALM!TOKENID=UUID).</summary>
        ApiToken
    }
}
