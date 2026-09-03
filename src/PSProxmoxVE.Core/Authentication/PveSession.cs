using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models;

namespace PSProxmoxVE.Core.Authentication
{
    /// <summary>Represents an authenticated session to a Proxmox VE server.</summary>
    public class PveSession
    {
        /// <summary>How long PVE honours a ticket from the moment it is issued.</summary>
        internal static readonly TimeSpan TicketLifetime = TimeSpan.FromHours(2);

        private readonly object _ticketLock = new object();
        private TicketState? _ticket;
        private Task<TicketState>? _renewal;

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

        /// <summary>The user (user@realm) the ticket was issued to; null for API token sessions.</summary>
        public string? Username { get; }

        /// <summary>The session ticket cookie value, when using ticket authentication.</summary>
        public string? Ticket => ReadTicket()?.Ticket;

        /// <summary>The CSRF prevention token, when using ticket authentication.</summary>
        public string? CsrfToken => ReadTicket()?.CsrfToken;

        /// <summary>The UTC expiry time for the session ticket.</summary>
        public DateTime TicketExpiry => ReadTicket()?.Expiry ?? DateTime.MaxValue;

        /// <summary>The Proxmox VE version detected on the server at connection time.</summary>
        public PveVersion? ServerVersion { get; internal set; }

        /// <summary>
        /// The default HTTP request timeout applied to clients created with this session.
        /// Defaults to 100 seconds (HttpClient's built-in default). Cmdlets that perform
        /// long-running operations (e.g. Send-PveFile) may override this per-call.
        /// </summary>
        public TimeSpan Timeout { get; internal set; } = TimeSpan.FromSeconds(100);

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
            string username,
            string ticket,
            string csrfToken,
            DateTime ticketExpiry)
        {
            Hostname = hostname;
            Port = port;
            SkipCertificateCheck = skipCertificateCheck;
            AuthMode = PveAuthMode.Ticket;
            Username = username ?? throw new ArgumentNullException(nameof(username));
            _ticket = new TicketState(ticket, csrfToken, ticketExpiry);
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
        }

        /// <summary>The current ticket credential as one consistent snapshot; null for API token sessions.</summary>
        internal TicketState? ReadTicket()
        {
            lock (_ticketLock)
                return _ticket;
        }

        /// <summary>
        /// Single-flight entry for replacing <paramref name="stale"/>. Returns the task every
        /// caller awaits for the outcome. When <paramref name="claimed"/> comes back non-null
        /// the caller owns the renewal and must finish it with <see cref="CompleteRenewal"/> or
        /// <see cref="FailRenewal"/>; otherwise it is joining one already in flight, or
        /// <paramref name="stale"/> has already been replaced and the task is the replacement.
        /// </summary>
        internal Task<TicketState> JoinOrClaimRenewal(TicketState stale, out TaskCompletionSource<TicketState>? claimed)
        {
            lock (_ticketLock)
            {
                claimed = null;
                if (!ReferenceEquals(_ticket, stale))
                    return Task.FromResult(_ticket!);
                if (_renewal != null)
                    return _renewal;

                claimed = new TaskCompletionSource<TicketState>(TaskCreationOptions.RunContinuationsAsynchronously);
                _renewal = claimed.Task;
                return _renewal;
            }
        }

        /// <summary>Installs <paramref name="renewed"/> and releases everyone awaiting the claimed renewal.</summary>
        internal void CompleteRenewal(TaskCompletionSource<TicketState> claimed, TicketState renewed)
        {
            if (renewed == null) throw new ArgumentNullException(nameof(renewed));
            lock (_ticketLock)
            {
                _ticket = renewed;
                _renewal = null;
            }
            claimed.SetResult(renewed);
        }

        /// <summary>Leaves the ticket as it was and hands <paramref name="failure"/> to everyone awaiting the claimed renewal.</summary>
        internal void FailRenewal(TaskCompletionSource<TicketState> claimed, Exception failure)
        {
            lock (_ticketLock)
                _renewal = null;
            claimed.SetException(failure);
        }

        /// <summary>
        /// One issued ticket: the cookie value, its CSRF token and its expiry. Immutable, so a
        /// request built from a snapshot never mixes fields from two different tickets.
        /// </summary>
        internal sealed class TicketState
        {
            public string Ticket { get; }
            public string CsrfToken { get; }
            public DateTime Expiry { get; }

            /// <summary>The instant after which a request should renew before sending: half the lifetime before expiry.</summary>
            public DateTime RenewAfter => Expiry - TimeSpan.FromTicks(TicketLifetime.Ticks / 2);

            public TicketState(string ticket, string csrfToken, DateTime expiry)
            {
                Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));
                CsrfToken = csrfToken ?? throw new ArgumentNullException(nameof(csrfToken));
                Expiry = expiry;
            }

            /// <summary>Parses the <c>data</c> envelope of a <c>POST /access/ticket</c> response.</summary>
            /// <param name="responseBody">The raw JSON response body.</param>
            /// <param name="issuedAt">When the ticket was issued; the expiry is <see cref="TicketLifetime"/> later.</param>
            public static TicketState FromTicketResponse(string responseBody, DateTime issuedAt)
            {
                var json = JObject.Parse(responseBody);
                if (!(json["data"] is JObject data))
                    throw new InvalidOperationException("Response did not contain a 'data' field.");

                var ticket = data["ticket"]?.Value<string>()
                    ?? throw new InvalidOperationException("Response did not contain a ticket.");
                var csrfToken = data["CSRFPreventionToken"]?.Value<string>()
                    ?? throw new InvalidOperationException("Response did not contain a CSRFPreventionToken.");

                return new TicketState(ticket, csrfToken, issuedAt + TicketLifetime);
            }
        }
    }
}
