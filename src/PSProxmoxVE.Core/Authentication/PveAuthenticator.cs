using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Core.Authentication
{
    /// <summary>Provides methods to authenticate against a Proxmox VE server</summary>
    public static class PveAuthenticator
    {
        // Pattern: USER@REALM!TOKENID=UUID
        private static readonly Regex ApiTokenRegex = new Regex(
            @"^[^@]+@[^!]+![^=]+=.+$",
            RegexOptions.Compiled);

        /// <summary>
        /// Authenticates using username and password, obtaining a ticket and CSRF token.
        /// The username must be in the form user@realm (e.g. root@pam).
        /// </summary>
        /// <param name="hostname">Hostname or IP address of the Proxmox VE server.</param>
        /// <param name="port">TCP port of the Proxmox VE API.</param>
        /// <param name="skipCertificateCheck">When true, skips TLS certificate validation.</param>
        /// <param name="username">Username including realm (e.g. root@pam).</param>
        /// <param name="password">Plain-text password for the user.</param>
        /// <param name="timeout">
        ///   Optional HTTP timeout to apply both to the authentication call and to subsequent
        ///   requests made with this session. When null, the default 100s applies.
        /// </param>
        public static PveSession AuthenticateWithCredentials(
            string hostname,
            int port,
            bool skipCertificateCheck,
            string username,
            string password,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                throw new ArgumentException("Hostname cannot be null or empty.", nameof(hostname));
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (!username.Contains("@"))
                throw new ArgumentException("Username must include a realm (e.g. 'root@pam').", nameof(username));
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            var formData = new Dictionary<string, string>
            {
                { "username", username },
                { "password", password }
            };

            string responseBody;
            using (var httpClient = new PveHttpClient(hostname, port, skipCertificateCheck, timeout))
            {
                responseBody = httpClient.Post("/api2/json/access/ticket", formData);
            }

            var json = JObject.Parse(responseBody);
            var data = json["data"] ?? throw new InvalidOperationException("Response did not contain a 'data' field.");

            var ticket = data["ticket"]?.Value<string>()
                ?? throw new InvalidOperationException("Response did not contain a ticket.");
            var csrfToken = data["CSRFPreventionToken"]?.Value<string>()
                ?? throw new InvalidOperationException("Response did not contain a CSRFPreventionToken.");

            var ticketExpiry = DateTime.UtcNow.AddHours(2);

            var session = new PveSession(hostname, port, skipCertificateCheck, ticket, csrfToken, ticketExpiry);
            if (timeout.HasValue)
                session.Timeout = timeout.Value;

            session.ServerVersion = GetVersion(session);

            return session;
        }

        /// <summary>
        /// Authenticates using a Proxmox VE API token.
        /// The token must be in the format USER@REALM!TOKENID=UUID (e.g. root@pam!mytoken=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).
        /// </summary>
        /// <param name="hostname">Hostname or IP address of the Proxmox VE server.</param>
        /// <param name="port">TCP port of the Proxmox VE API.</param>
        /// <param name="skipCertificateCheck">When true, skips TLS certificate validation.</param>
        /// <param name="apiToken">API token in USER@REALM!TOKENID=UUID format.</param>
        /// <param name="timeout">
        ///   Optional HTTP timeout to apply to requests made with this session.
        ///   When null, the default 100s applies.
        /// </param>
        public static PveSession AuthenticateWithApiToken(
            string hostname,
            int port,
            bool skipCertificateCheck,
            string apiToken,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                throw new ArgumentException("Hostname cannot be null or empty.", nameof(hostname));
            if (string.IsNullOrWhiteSpace(apiToken))
                throw new ArgumentException("API token cannot be null or empty.", nameof(apiToken));
            if (!ApiTokenRegex.IsMatch(apiToken))
                throw new ArgumentException(
                    "API token must be in the format 'USER@REALM!TOKENID=UUID' (e.g. root@pam!mytoken=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).",
                    nameof(apiToken));

            var session = new PveSession(hostname, port, skipCertificateCheck, apiToken);
            if (timeout.HasValue)
                session.Timeout = timeout.Value;

            session.ServerVersion = GetVersion(session);

            return session;
        }

        /// <summary>Retrieves and parses the server version from the PVE API</summary>
        private static PveVersion GetVersion(PveSession session)
        {
            string responseBody;
            using (var httpClient = new PveHttpClient(session))
            {
                responseBody = httpClient.Get("version");
            }

            var json = JObject.Parse(responseBody);
            var data = json["data"] ?? throw new InvalidOperationException("Version response did not contain a 'data' field.");

            var versionString = data["version"]?.Value<string>()
                ?? throw new InvalidOperationException("Version response did not contain a 'version' field.");

            return PveVersion.Parse(versionString);
        }
    }
}
