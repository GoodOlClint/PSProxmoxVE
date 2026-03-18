using System;
using PSProxmoxVE.Core.Models;

namespace PSProxmoxVE.Core.Authentication
{
    /// <summary>Represents an authenticated session to a Proxmox VE server</summary>
    public class PveSession
    {
        public string Hostname { get; }
        public int Port { get; }
        public bool SkipCertificateCheck { get; }
        public PveAuthMode AuthMode { get; }
        public string? ApiToken { get; }
        public string? Ticket { get; }
        public string? CsrfToken { get; }
        public DateTime TicketExpiry { get; }
        public PveVersion ServerVersion { get; internal set; }

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
