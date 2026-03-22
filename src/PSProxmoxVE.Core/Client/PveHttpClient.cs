using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Exceptions;

#if NET48 || NETSTANDARD2_0
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
#endif

namespace PSProxmoxVE.Core.Client
{
    /// <summary>
    /// Low-level HTTP client for communicating with the Proxmox VE API.
    /// Handles authentication headers, error parsing, and the ISO upload workaround.
    /// </summary>
    public class PveHttpClient : IDisposable
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        private readonly PveSession? _session;
#pragma warning restore CS8625
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        private const string ApiTokenPrefix = "PVEAPIToken=";
        private const string AuthCookieName = "PVEAuthCookie=";
        private const string CsrfHeaderName = "CSRFPreventionToken";

        /// <summary>
        /// Creates an HTTP client authenticated with the specified PVE session.
        /// </summary>
        /// <param name="session">The authenticated PVE session providing credentials and base URL.</param>
        public PveHttpClient(PveSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _baseUrl = session.BaseUrl;

#if NET48 || NETSTANDARD2_0
            var handler = new HttpClientHandler();
            if (session.SkipCertificateCheck)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (HttpRequestMessage _, X509Certificate2 _, X509Chain _, SslPolicyErrors _) => true;
            }
            _httpClient = new HttpClient(handler);
#else
            if (session.SkipCertificateCheck)
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        (_, _, _, _) => true
                };
                _httpClient = new HttpClient(handler);
            }
            else
            {
                _httpClient = new HttpClient();
            }
#endif

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Creates a bare HTTP client for pre-session use (e.g. initial authentication).
        /// No auth headers are added to requests made with this constructor.
        /// </summary>
        internal PveHttpClient(string hostname, int port, bool skipCertificateCheck)
        {
            if (string.IsNullOrWhiteSpace(hostname))
                throw new ArgumentException("Hostname cannot be null or empty.", nameof(hostname));

            _session = null;
            _baseUrl = $"https://{hostname}:{port}";

#if NET48 || NETSTANDARD2_0
            var handler = new HttpClientHandler();
            if (skipCertificateCheck)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (HttpRequestMessage _, X509Certificate2 _, X509Chain _, SslPolicyErrors _) => true;
            }
            _httpClient = new HttpClient(handler);
#else
            if (skipCertificateCheck)
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        (_, _, _, _) => true
                };
                _httpClient = new HttpClient(handler);
            }
            else
            {
                _httpClient = new HttpClient();
            }
#endif

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
            var request = BuildRequest(HttpMethod.Get, resource);
            return await SendAsync(request, resource, "GET").ConfigureAwait(false);
        }

        /// <summary>Performs a POST request against the specified API resource path.</summary>
        /// <param name="resource">Relative resource path</param>
        /// <param name="data">Form fields to send as application/x-www-form-urlencoded body</param>
        /// <returns>Raw JSON response body</returns>
        public async Task<string> PostAsync(string resource, Dictionary<string, string>? data = null)
        {
            var request = BuildRequest(HttpMethod.Post, resource, mutating: true);
            if (data != null)
                request.Content = new FormUrlEncodedContent(data);
            return await SendAsync(request, resource, "POST").ConfigureAwait(false);
        }

        /// <summary>Performs a PUT request against the specified API resource path.</summary>
        /// <param name="resource">Relative resource path</param>
        /// <param name="data">Form fields to send as application/x-www-form-urlencoded body</param>
        /// <returns>Raw JSON response body</returns>
        public async Task<string> PutAsync(string resource, Dictionary<string, string>? data = null)
        {
            var request = BuildRequest(HttpMethod.Put, resource, mutating: true);
            if (data != null)
                request.Content = new FormUrlEncodedContent(data);
            return await SendAsync(request, resource, "PUT").ConfigureAwait(false);
        }

        /// <summary>Performs a DELETE request against the specified API resource path.</summary>
        /// <param name="resource">Relative resource path</param>
        /// <returns>Raw JSON response body</returns>
        public async Task<string> DeleteAsync(string resource)
        {
            var request = BuildRequest(HttpMethod.Delete, resource, mutating: true);
            return await SendAsync(request, resource, "DELETE").ConfigureAwait(false);
        }

        // -------------------------------------------------------------------------
        // Synchronous wrappers
        // -------------------------------------------------------------------------

        /// <summary>Synchronous wrapper for <see cref="GetAsync"/>.</summary>
        public string Get(string resource) =>
            GetAsync(resource).GetAwaiter().GetResult();

        /// <summary>Synchronous wrapper for <see cref="PostAsync"/>.</summary>
        public string Post(string resource, Dictionary<string, string>? data = null) =>
            PostAsync(resource, data).GetAwaiter().GetResult();

        /// <summary>Synchronous wrapper for <see cref="PutAsync"/>.</summary>
        public string Put(string resource, Dictionary<string, string>? data = null) =>
            PutAsync(resource, data).GetAwaiter().GetResult();

        /// <summary>Synchronous wrapper for <see cref="DeleteAsync"/>.</summary>
        public string Delete(string resource) =>
            DeleteAsync(resource).GetAwaiter().GetResult();

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

                var request = BuildRequest(HttpMethod.Post, resource, mutating: true);
                request.Content = multipart;

                return await SendAsync(request, resource, "POST").ConfigureAwait(false);
            }
            finally
            {
#if NET48 || NETSTANDARD2_0
                fileStream.Dispose();
#else
                await fileStream.DisposeAsync().ConfigureAwait(false);
#endif
            }
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private HttpRequestMessage BuildRequest(HttpMethod method, string resource, bool mutating = false)
        {
            var url = _baseUrl + resource;
            var request = new HttpRequestMessage(method, url);

            if (_session == null)
                return request;

            if (_session.AuthMode == PveAuthMode.ApiToken)
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"{ApiTokenPrefix}{_session.ApiToken}");
            }
            else
            {
                // Ticket auth
                request.Headers.Add("Cookie", $"{AuthCookieName}{_session.Ticket}");
                if (mutating && !string.IsNullOrEmpty(_session.CsrfToken))
                    request.Headers.Add(CsrfHeaderName, _session.CsrfToken);
            }

            return request;
        }

        private async Task<string> SendAsync(HttpRequestMessage request, string resource, string httpMethod)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new PveApiException(HttpStatusCode.ServiceUnavailable,
                    ex.Message, resource, httpMethod, ex);
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

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
#if NET48 || NETSTANDARD2_0
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
#else
            RandomNumberGenerator.Fill(bytes);
#endif
            var sb = new StringBuilder(32);
            for (int i = 0; i < 32; i++)
                sb.Append(chars[bytes[i] % chars.Length]);
            return sb.ToString();
        }

        /// <inheritdoc />
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
