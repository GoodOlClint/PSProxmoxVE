using System;
using PSProxmoxVE.Core.Models;

namespace PSProxmoxVE.Core.Authentication
{
    /// <summary>Represents an authenticated session to a Proxmox VE server.</summary>
    public class PveSession
    {
        /// <summary>The hostname or IP address of the Proxmox VE server.</summary>
        public string Hostname { get; }

        /// <summary>The TCP port of the Proxmox VE API (default 8006).</summary>
        public int Port { get; }

        /// <summary>Whether to skip TLS certificate validation when connecting.</summary>
        public bool SkipCertificateCheck { get; }

        /// <summary>The authentication mode used for this session.</summary>
        public PveAuthMode AuthMode { get; }

        /// <summary>The API token string, when using API token authentication.</summary>
        public string? ApiToken { get; }

        /// <summary>The session ticket cookie value, when using ticket authentication.</summary>
        public string? Ticket { get; }

        /// <summary>The CSRF prevention token, when using ticket authentication.</summary>
        public string? CsrfToken { get; }

        /// <summary>The UTC expiry time for the session ticket.</summary>
        public DateTime TicketExpiry { get; }

        /// <summary>The Proxmox VE version detected on the server at connection time.</summary>
        public PveVersion? ServerVersion { get; internal set; }

        /// <summary>Returns true if the ticket has expired (only relevant for Ticket auth mode)</summary>
        public bool IsExpired
        {
            get
            {
                if (AuthMode == PveAuthMode.ApiToken)
                    return false;
                return DateTime.UtcNow >= TicketExpiry;
            }
        }

        /// <summary>Base URL for the Proxmox VE API</summary>
        public string BaseUrl => $"https://{Hostname}:{Port}/api2/json/";

        /// <summary>Creates a session using ticket-based authentication</summary>
        internal PveSession(
            string hostname,
            int port,
            bool skipCertificateCheck,
            string ticket,
            string csrfToken,
            DateTime ticketExpiry)
        {
            Hostname = hostname;
            Port = port;
            SkipCertificateCheck = skipCertificateCheck;
            AuthMode = PveAuthMode.Ticket;
            Ticket = ticket;
            CsrfToken = csrfToken;
            TicketExpiry = ticketExpiry;
        }

        /// <summary>Creates a session using API token authentication</summary>
        internal PveSession(
            string hostname,
            int port,
            bool skipCertificateCheck,
            string apiToken)
        {
            Hostname = hostname;
            Port = port;
            SkipCertificateCheck = skipCertificateCheck;
            AuthMode = PveAuthMode.ApiToken;
            ApiToken = apiToken;
            TicketExpiry = DateTime.MaxValue;
        }
    }
}
