using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Client
{
    /// <summary>
    /// Low-level HTTP client for communicating with the Proxmox VE API.
    /// Handles authentication headers, error parsing, and the ISO upload workaround.
    /// </summary>
    public class PveHttpClient : IPveHttpClient
    {
        private readonly PveSession? _session;
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private readonly HttpMessageHandler _handler;
        private bool _disposed;

        private readonly TimeSpan _guestLockRetryWindow;
        private readonly Func<TimeSpan, Task> _guestLockRetryDelay;
        private readonly Func<DateTime> _utcNow;

        private const string ApiTokenPrefix = "PVEAPIToken=";
        private const string AuthCookieName = "PVEAuthCookie=";
        private const string CsrfHeaderName = "CSRFPreventionToken";
        private const string TicketResource = "access/ticket";

        /// <summary>
        /// Creates an HTTP client authenticated with the specified PVE session.
        /// </summary>
        /// <param name="session">The authenticated PVE session providing credentials and base URL.</param>
        /// <param name="timeoutOverride">
        ///   Optional per-instance timeout override. When supplied, takes precedence over
        ///   <see cref="PveSession.Timeout"/>. Pass <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>
        ///   to disable the timeout entirely (useful for multi-GB uploads/downloads).
        /// </param>
        public PveHttpClient(PveSession session, TimeSpan? timeoutOverride = null)
            : this(session, timeoutOverride, guestLockRetryWindow: null, handler: null, guestLockRetryDelay: null, handlerCache: null)
        {
        }

        /// <summary>
        /// Test seam: same as the public constructor but drawing the transport handler from
        /// <paramref name="handlerCache"/> instead of <see cref="PveHandlerCache.Shared"/>.
        /// </summary>
        /// <param name="session">The authenticated PVE session providing credentials and base URL.</param>
        /// <param name="handlerCache">The cache to take the handler from.</param>
        internal PveHttpClient(PveSession session, PveHandlerCache handlerCache)
            : this(session, timeoutOverride: null, guestLockRetryWindow: null, handler: null, guestLockRetryDelay: null,
                  handlerCache: handlerCache ?? throw new ArgumentNullException(nameof(handlerCache)))
        {
        }

        /// <summary>
        /// Test seam: builds a client against an explicit handler, lock-retry window and/or
        /// inter-attempt delay. Production code always goes through the public constructor.
        /// An explicit handler is owned by this client and bypasses the shared handler cache.
        /// </summary>
        /// <param name="session">The authenticated PVE session providing credentials and base URL.</param>
        /// <param name="timeoutOverride">Optional per-instance timeout override.</param>
        /// <param name="guestLockRetryWindow">
        ///   Retry budget passed to <see cref="GuestLockRetry.ExecuteAsync{T}(Func{Task{T}}, TimeSpan?, Func{TimeSpan, Task})"/>.
        ///   Null uses <see cref="GuestLockRetry.DefaultWindow"/>, the same as the public constructor.
        /// </param>
        /// <param name="handler">
        ///   Message handler to send requests through, owned and disposed by this client. Null
        ///   takes the shared pooled handler for the session's endpoint from the handler cache.
        /// </param>
        /// <param name="guestLockRetryDelay">
        ///   Invoked before each guest-lock reissue instead of sleeping. Null uses
        ///   <see cref="Task.Delay(TimeSpan)"/>, the same as the public constructor.
        /// </param>
        /// <param name="handlerCache">
        ///   Cache to take the handler from when <paramref name="handler"/> is null. Null uses
        ///   <see cref="PveHandlerCache.Shared"/>, the same as the public constructor.
        /// </param>
        /// <param name="utcNow">
        ///   Clock the ticket half-life check reads. Null uses <see cref="DateTime.UtcNow"/>,
        ///   the same as the public constructor.
        /// </param>
        internal PveHttpClient(
            PveSession session,
            TimeSpan? timeoutOverride,
            TimeSpan? guestLockRetryWindow,
            HttpMessageHandler? handler,
            Func<TimeSpan, Task>? guestLockRetryDelay = null,
            PveHandlerCache? handlerCache = null,
            Func<DateTime>? utcNow = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _baseUrl = session.BaseUrl;
            _guestLockRetryWindow = guestLockRetryWindow ?? GuestLockRetry.DefaultWindow;
            _guestLockRetryDelay = guestLockRetryDelay ?? Task.Delay;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);

            var ownsHandler = handler != null;
            _handler = handler ?? (handlerCache ?? PveHandlerCache.Shared)
                .Get(session.Hostname, session.Port, session.SkipCertificateCheck);
            _httpClient = new HttpClient(_handler, disposeHandler: ownsHandler);
            _httpClient.Timeout = timeoutOverride ?? session.Timeout;

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>The transport handler requests go through.</summary>
        internal HttpMessageHandler Handler => _handler;

        /// <summary>
        /// Creates a bare HTTP client for pre-session use (e.g. initial authentication).
        /// Requests it builds carry no authentication headers.
        /// </summary>
        internal PveHttpClient(string hostname, int port, bool skipCertificateCheck, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                throw new ArgumentException("Hostname cannot be null or empty.", nameof(hostname));

            _session = null;
            _baseUrl = $"https://{hostname}:{port}";
            _guestLockRetryWindow = GuestLockRetry.DefaultWindow;
            _guestLockRetryDelay = Task.Delay;
            _utcNow = () => DateTime.UtcNow;

            _handler = PveHandlerCache.Shared.Get(hostname, port, skipCertificateCheck);
            _httpClient = new HttpClient(_handler, disposeHandler: false);
            if (timeout.HasValue)
                _httpClient.Timeout = timeout.Value;

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // -------------------------------------------------------------------------
        // Core request methods
        // -------------------------------------------------------------------------

        /// <summary>Performs a GET request against the specified API resource path.</summary>
        /// <param name="resource">Relative resource path, e.g. "/nodes/pve/status"</param>
        /// <returns>Raw JSON response body</returns>
        public async Task<string> GetAsync(string resource)
        {
            return await SendAsync(ticket => BuildRequest(HttpMethod.Get, resource, ticket), resource, "GET")
                .ConfigureAwait(false);
        }

        /// <summary>Performs a POST request against the specified API resource path.</summary>
        /// <param name="resource">Relative resource path</param>
        /// <param name="data">Form fields to send as application/x-www-form-urlencoded body</param>
        /// <returns>Raw JSON response body</returns>
        public async Task<string> PostAsync(string resource, Dictionary<string, string>? data = null)
        {
            return await SendAsync(ticket =>
            {
                var request = BuildRequest(HttpMethod.Post, resource, ticket, mutating: true);
                if (data != null)
                    request.Content = BuildFormContent(data);
                return request;
            }, resource, "POST").ConfigureAwait(false);
        }

        /// <summary>
        /// POST whose form body may contain repeated keys, for PVE array parameters
        /// (e.g. guest-exec "command"). Each pair becomes one key=value field.
        /// </summary>
        /// <param name="resource">Relative resource path</param>
        /// <param name="data">Form fields; a key may appear more than once</param>
        /// <returns>Raw JSON response body</returns>
        public async Task<string> PostAsync(string resource, IEnumerable<KeyValuePair<string, string>> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return await SendAsync(ticket =>
            {
                var request = BuildRequest(HttpMethod.Post, resource, ticket, mutating: true);
                request.Content = BuildFormContent(data);
                return request;
            }, resource, "POST").ConfigureAwait(false);
        }

        /// <summary>Performs a PUT request against the specified API resource path.</summary>
        /// <param name="resource">Relative resource path</param>
        /// <param name="data">Form fields to send as application/x-www-form-urlencoded body</param>
        /// <returns>Raw JSON response body</returns>
        public async Task<string> PutAsync(string resource, Dictionary<string, string>? data = null)
        {
            return await SendAsync(ticket =>
            {
                var request = BuildRequest(HttpMethod.Put, resource, ticket, mutating: true);
                if (data != null)
                    request.Content = BuildFormContent(data);
                return request;
            }, resource, "PUT").ConfigureAwait(false);
        }

        /// <summary>Performs a DELETE request against the specified API resource path.</summary>
        /// <param name="resource">Relative resource path</param>
        /// <returns>Raw JSON response body</returns>
        public async Task<string> DeleteAsync(string resource)
        {
            return await SendAsync(ticket => BuildRequest(HttpMethod.Delete, resource, ticket, mutating: true), resource, "DELETE")
                .ConfigureAwait(false);
        }

        // -------------------------------------------------------------------------
        // Form body encoding
        // -------------------------------------------------------------------------

        /// <summary>
        /// Builds form-encoded content using minimal encoding that matches curl behavior.
        /// .NET's <see cref="FormUrlEncodedContent"/> over-encodes characters like
        /// <c>:</c> and <c>!</c> which PVE's internal API consumers (e.g. cluster join)
        /// do not properly URL-decode.
        /// </summary>
        private static StringContent BuildFormContent(IEnumerable<KeyValuePair<string, string>> data)
        {
            var sb = new StringBuilder();
            foreach (var kvp in data)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.Append(EncodeFormValue(kvp.Key));
                sb.Append('=');
                sb.Append(EncodeFormValue(kvp.Value));
            }
            return new StringContent(sb.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
        }

        /// <summary>
        /// Encodes a form value with minimal percent-encoding — only characters that
        /// would break form parsing are encoded. This matches curl's <c>-d</c> behavior
        /// and avoids over-encoding that PVE does not handle correctly.
        /// </summary>
        private static string EncodeFormValue(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '&': sb.Append("%26"); break;
                    case '=': sb.Append("%3D"); break;
                    // PVE's form parser treats a raw ';' as a field separator (the
                    // historical alternative to '&'), so an unencoded ';' inside a value
                    // (e.g. boot=order=scsi0;ide2) splits the value into bogus extra
                    // fields. Encode it so the value arrives intact.
                    case ';': sb.Append("%3B"); break;
                    case '+': sb.Append("%2B"); break;
                    case ' ': sb.Append('+'); break;
                    case '%': sb.Append("%25"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        // -------------------------------------------------------------------------
        // Synchronous wrappers
        // -------------------------------------------------------------------------

        /// <summary>Synchronous wrapper for <see cref="GetAsync"/>.</summary>
        public string Get(string resource) =>
            GetAsync(resource).GetAwaiter().GetResult();

        /// <summary>Synchronous wrapper for <see cref="PostAsync(string, Dictionary{string, string})"/>.</summary>
        public string Post(string resource, Dictionary<string, string>? data = null) =>
            PostAsync(resource, data).GetAwaiter().GetResult();

        // -------------------------------------------------------------------------
        // ISO / file upload
        // -------------------------------------------------------------------------

        /// <summary>
        /// Uploads a file (e.g. an ISO) to a Proxmox VE storage endpoint using
        /// MultipartFormDataContent.
        ///
        /// IMPORTANT — Bugzilla 7389 workaround:
        ///   https://bugzilla.proxmox.com/show_bug.cgi?id=7389
        ///
        ///   The Proxmox VE API rejects uploads when the multipart body contains
        ///   "Content-Type" or "Content-Transfer-Encoding" headers on text parts,
        ///   and also rejects a quoted boundary in the Content-Type header.
        ///
        ///   Workaround:
        ///     * Override the multipart Content-Type header to use an unquoted boundary.
        ///     * Set ContentType = null on all StringContent text parts before adding them.
        ///     * Use StreamContent for the file part (no Content-Transfer-Encoding added).
        /// </summary>
        /// <param name="resource">Relative API resource path, e.g. "nodes/pve/storage/local/upload"</param>
        /// <param name="filePath">Absolute local path to the file to upload</param>
        /// <param name="formFields">Additional form fields (e.g. content type)</param>
        /// <param name="checksum">Optional file checksum value</param>
        /// <param name="checksumAlgorithm">Optional checksum algorithm name (e.g. "sha256")</param>
        /// <param name="progressCallback">
        ///   Optional callback invoked periodically with (bytesSent, totalBytes).
        ///   May be called from a background thread.
        /// </param>
        /// <returns>Raw JSON response body</returns>
        public async Task<string> UploadFileAsync(
            string resource,
            string filePath,
            Dictionary<string, string>? formFields = null,
            string? checksum = null,
            string? checksumAlgorithm = null,
            Action<long, long>? progressCallback = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must not be null or empty.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Upload file not found.", filePath);

            var boundary = GenerateBoundary();
            var fileName = Path.GetFileName(filePath);
            var totalBytes = new FileInfo(filePath).Length;

            var multipart = new MultipartFormDataContent(boundary);

            // Override Content-Type to use unquoted boundary (PVE rejects quoted boundaries).
            multipart.Headers.ContentType =
                MediaTypeHeaderValue.Parse($"multipart/form-data; boundary={boundary}");

            // Add text form fields.
            // IMPORTANT — BZ 7389 workaround: PVE requires quoted name= values in
            // Content-Disposition (e.g. name="content"), but .NET's
            // MultipartFormDataContent.Add(part, name) emits name=content (unquoted),
            // which PVE rejects with a broken-pipe / stream-copy error.
            // Fix: set ContentDisposition manually with embedded quotes, then call
            // multipart.Add(part) without a name so the header is not overwritten.
            if (formFields != null)
            {
                foreach (var kvp in formFields)
                {
                    var part = new StringContent(kvp.Value, Encoding.UTF8);
                    part.Headers.ContentType = null;
                    part.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = $"\"{kvp.Key}\""
                    };
                    multipart.Add(part);
                }
            }

            if (!string.IsNullOrEmpty(checksumAlgorithm) && !string.IsNullOrEmpty(checksum))
            {
                var algPart = new StringContent(checksumAlgorithm!, Encoding.UTF8);
                algPart.Headers.ContentType = null;
                algPart.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "\"checksum-algorithm\"" };
                multipart.Add(algPart);
            }

            if (!string.IsNullOrEmpty(checksum))
            {
                var csPart = new StringContent(checksum!, Encoding.UTF8);
                csPart.Headers.ContentType = null;
                csPart.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "\"checksum\"" };
                multipart.Add(csPart);
            }

            var ticket = await CurrentTicketAsync().ConfigureAwait(false);

            // File part — StreamContent does not add Content-Transfer-Encoding.
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4 * 1024 * 1024, useAsync: true);
            try
            {
                Stream uploadStream = progressCallback != null
                    ? (Stream)new ProgressStream(fileStream, totalBytes, progressCallback)
                    : fileStream;

                // ContentDisposition MUST come before ContentType in the part headers.
                // PVE closes the connection if ContentType appears first (server-side parse order sensitivity).
                var fileContent = new StreamContent(uploadStream);
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"filename\"",
                    FileName = $"\"{fileName}\""
                };
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                multipart.Add(fileContent);

                var request = BuildRequest(HttpMethod.Post, resource, ticket, mutating: true);
                request.Content = multipart;

                return await SendOnceAsync(request, resource, "POST").ConfigureAwait(false);
            }
            finally
            {
                fileStream.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Builds a request signed with <paramref name="ticket"/> on a ticket-mode session, or
        /// with the API token otherwise. The ticket is passed in rather than read from the
        /// session so the caller knows which ticket the request carried when it comes back 401.
        /// </summary>
        private HttpRequestMessage BuildRequest(HttpMethod method, string resource, PveSession.TicketState? ticket, bool mutating = false)
        {
            var url = _baseUrl + resource;
            var request = new HttpRequestMessage(method, url);

            if (_session == null)
                return request;

            if (_session.AuthMode == PveAuthMode.ApiToken)
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"{ApiTokenPrefix}{_session.ApiToken}");
            }
            else if (ticket != null)
            {
                request.Headers.Add("Cookie", $"{AuthCookieName}{ticket.Ticket}");
                if (mutating && !string.IsNullOrEmpty(ticket.CsrfToken))
                    request.Headers.Add(CsrfHeaderName, ticket.CsrfToken);
            }

            return request;
        }

        /// <summary>
        /// Sends a request, rebuilding it from <paramref name="buildRequest"/> for each attempt
        /// while PVE rejects it for a guest's config flock, and once more after a ticket renewal
        /// when a ticket-mode session is rejected with 401. An <see cref="HttpRequestMessage"/>
        /// cannot be resent, which is why this takes a factory rather than a request.
        /// </summary>
        private Task<string> SendAsync(Func<PveSession.TicketState?, HttpRequestMessage> buildRequest, string resource, string httpMethod) =>
            GuestLockRetry.ExecuteAsync(
                () => SendRenewingAsync(buildRequest, resource, httpMethod), _guestLockRetryWindow, _guestLockRetryDelay);

        private async Task<string> SendRenewingAsync(Func<PveSession.TicketState?, HttpRequestMessage> buildRequest, string resource, string httpMethod)
        {
            var ticket = await CurrentTicketAsync().ConfigureAwait(false);
            try
            {
                return await SendOnceAsync(buildRequest(ticket), resource, httpMethod).ConfigureAwait(false);
            }
            catch (PveApiException ex) when (ticket != null && ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                var renewed = await RenewTicketAsync(ticket, ex).ConfigureAwait(false);
                try
                {
                    return await SendOnceAsync(buildRequest(renewed), resource, httpMethod).ConfigureAwait(false);
                }
                catch (PveApiException again) when (again.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new PveSessionExpiredException("The renewed ticket was rejected too.", again);
                }
            }
        }

        /// <summary>
        /// The ticket a request should carry now: null for a bare client or an API token session,
        /// otherwise the session's current ticket, renewed first if it is past half its lifetime.
        /// A ticket past its full lifetime has nothing left to trade, so it is not sent anywhere.
        /// </summary>
        private async Task<PveSession.TicketState?> CurrentTicketAsync()
        {
            if (_session == null || _session.AuthMode != PveAuthMode.Ticket)
                return null;

            var ticket = _session.ReadTicket()!;
            var now = _utcNow();
            if (now < ticket.RenewAfter)
                return ticket;
            if (now >= ticket.Expiry)
                throw new PveSessionExpiredException();

            return await RenewTicketAsync(ticket, rejection: null).ConfigureAwait(false);
        }

        /// <summary>
        /// Trades <paramref name="stale"/> for a fresh ticket by posting it as the password to
        /// <c>/access/ticket</c>, and installs the result on the session. Concurrent callers on
        /// the same session share one POST and its outcome, success or failure; a caller whose
        /// ticket was already replaced gets the replacement without any POST.
        /// </summary>
        /// <param name="stale">The ticket the caller holds and wants replaced.</param>
        /// <param name="rejection">
        ///   The 401 that prompted this renewal, when reactive. A failed reactive renewal throws
        ///   <see cref="PveSessionExpiredException"/> around it. A failed proactive renewal does
        ///   so only when the ticket endpoint itself answers 401 or the ticket has meanwhile
        ///   expired; any other failure says nothing about the ticket, which is still valid, so
        ///   the caller keeps using it.
        /// </param>
        private async Task<PveSession.TicketState> RenewTicketAsync(PveSession.TicketState stale, PveApiException? rejection)
        {
            var session = _session!;
            var renewal = session.JoinOrClaimRenewal(stale, out var claimed);
            if (claimed != null)
            {
                try
                {
                    session.CompleteRenewal(claimed, await PostTicketRenewalAsync(session, stale).ConfigureAwait(false));
                }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
                {
                    session.FailRenewal(claimed, ex);
                }
            }

            try
            {
                return await renewal.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is PveApiException or Newtonsoft.Json.JsonException or InvalidOperationException)
            {
                if (rejection != null)
                    throw new PveSessionExpiredException($"Ticket renewal failed: {ex.Message}", rejection);
                if (ex is PveApiException api && api.StatusCode == HttpStatusCode.Unauthorized)
                    throw new PveSessionExpiredException(ex);
                if (_utcNow() < stale.Expiry)
                    return stale;
                throw new PveSessionExpiredException($"Ticket renewal failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// The renewal POST itself. It goes straight to <see cref="SendOnceAsync"/> so it carries
        /// no cookie and cannot recurse into renewal or the guest-lock retry, and it is bounded by
        /// the session's own timeout rather than this client's, which an upload may have set to
        /// infinite.
        /// </summary>
        private async Task<PveSession.TicketState> PostTicketRenewalAsync(PveSession session, PveSession.TicketState stale)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + TicketResource);
            request.Content = BuildFormContent(new Dictionary<string, string>
            {
                ["username"] = session.Username!,
                ["password"] = stale.Ticket,
            });

            var issuedAt = _utcNow();
            var body = await SendOnceAsync(request, TicketResource, "POST", session.Timeout).ConfigureAwait(false);
            return PveSession.TicketState.FromTicketResponse(body, issuedAt);
        }

        private async Task<string> SendOnceAsync(HttpRequestMessage request, string resource, string httpMethod, TimeSpan? timeout = null)
        {
            HttpResponseMessage response;
            string body;
            using (var cts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : null)
            {
                try
                {
                    response = await _httpClient.SendAsync(request, cts?.Token ?? CancellationToken.None).ConfigureAwait(false);
                    body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (TaskCanceledException ex)
                {
                    // The only token ever handed to HttpClient.SendAsync is the per-call timeout
                    // above, so a TaskCanceledException reaching here is always a timeout — that
                    // one or HttpClient.Timeout — on .NET Framework, on .NET Core, and on .NET 5+
                    // (where it also carries a TimeoutException inner). Wrap it uniformly.
                    var limit = timeout ?? _httpClient.Timeout;
                    var seconds = limit == System.Threading.Timeout.InfiniteTimeSpan
                        ? "infinite"
                        : limit.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "s";
                    throw new PveApiException(HttpStatusCode.RequestTimeout,
                        $"Request timed out after {seconds}.", resource, httpMethod, ex);
                }
                catch (HttpRequestException ex)
                {
                    // Covers both a failed connection and a stream drop mid-body-read, so every
                    // HttpRequestException PveHttpClient can throw arrives as PveApiException.
                    throw new PveApiException(HttpStatusCode.ServiceUnavailable,
                        ex.Message, resource, httpMethod, ex);
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = ExtractErrorMessage(body, response.ReasonPhrase ?? response.StatusCode.ToString());
                throw new PveApiException(response.StatusCode, errorMessage, resource, httpMethod);
            }

            return body;
        }

        /// <summary>
        /// Attempts to extract a human-readable error message from the PVE API JSON response.
        /// PVE wraps errors in <c>{"errors": {"field": "message", ...}}</c> inside the standard
        /// <c>{"data": null, "errors": {...}}</c> envelope.
        /// </summary>
        private static string ExtractErrorMessage(string body, string fallback)
        {
            if (string.IsNullOrWhiteSpace(body))
                return fallback;

            try
            {
                var json = JObject.Parse(body);

                // Top-level "errors" object
                var errors = json["errors"] as JObject;
                if (errors != null && errors.Count > 0)
                {
                    var parts = new List<string>();
                    foreach (var prop in errors.Properties())
                        parts.Add($"{prop.Name}: {prop.Value}");
                    return string.Join("; ", parts);
                }

                // Some endpoints return a plain string in "message"
                var message = json["message"]?.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                    return message!;
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // If parsing fails, fall through to returning the raw body (truncated)
            }

            // Return raw body, truncated to avoid enormous exception messages
            return body.Length > 512 ? body.Substring(0, 512) + "..." : body;
        }

        /// <summary>Generates a random 32-character alphanumeric boundary string using a cryptographic RNG.</summary>
        private static string GenerateBoundary()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            var sb = new StringBuilder(32);
            for (int i = 0; i < 32; i++)
                sb.Append(chars[bytes[i] % chars.Length]);
            return sb.ToString();
        }

        /// <summary>
        /// Releases this client. A handler taken from the shared cache stays alive for the
        /// other clients on the same endpoint; only an explicitly supplied handler is disposed.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
        }

        // -------------------------------------------------------------------------
        // Helper: progress-reporting stream wrapper for uploads
        // -------------------------------------------------------------------------

        /// <summary>
        /// Wraps an inner stream and invokes a progress callback as bytes are read,
        /// enabling upload progress reporting for file uploads.
        /// </summary>
        private sealed class ProgressStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _totalBytes;
            private readonly Action<long, long> _callback;
            private long _bytesRead;

            public ProgressStream(Stream inner, long totalBytes, Action<long, long> callback)
            {
                _inner = inner;
                _totalBytes = totalBytes;
                _callback = callback;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush();
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

            public override int Read(byte[] buffer, int offset, int count)
            {
                var n = _inner.Read(buffer, offset, count);
                if (n > 0) { _bytesRead += n; _callback(_bytesRead, _totalBytes); }
                return n;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _inner.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
