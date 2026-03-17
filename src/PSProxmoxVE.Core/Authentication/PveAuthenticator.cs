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
        public static PveSession AuthenticateWithCredentials(
            string hostname,
            int port,
            bool skipCertificateCheck,
            string username,
            string password)
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
            using (var httpClient = new PveHttpClient(hostname, port, skipCertificateCheck))
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

            session.ServerVersion = GetVersion(session);

            return session;
        }

        /// <summary>
        /// Authenticates using a Proxmox VE API token.
        /// The token must be in the format USER@REALM!TOKENID=UUID (e.g. root@pam!mytoken=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).
        /// </summary>
        public static PveSession AuthenticateWithApiToken(
            string hostname,
            int port,
            bool skipCertificateCheck,
            string apiToken)
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

            session.ServerVersion = GetVersion(session);

            return session;
        }

        /// <summary>Retrieves and parses the server version from the PVE API</summary>
        private static PveVersion GetVersion(PveSession session)
        {
            string responseBody;
            using (var httpClient = new PveHttpClient(session))
            {
                responseBody = httpClient.Get("/api2/json/version");
            }

            var json = JObject.Parse(responseBody);
            var data = json["data"] ?? throw new InvalidOperationException("Version response did not contain a 'data' field.");

            var versionString = data["version"]?.Value<string>()
                ?? throw new InvalidOperationException("Version response did not contain a 'version' field.");

            return PveVersion.Parse(versionString);
        }
    }
}
