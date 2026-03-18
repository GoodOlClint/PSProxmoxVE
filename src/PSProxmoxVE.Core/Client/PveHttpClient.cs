using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Exceptions;

#if NET48
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

        public PveHttpClient(PveSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _baseUrl = session.BaseUrl;

#if NET48
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

#if NET48
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
        /// Uploads a file (e.g. an ISO) to a Proxmox VE storage endpoint using a manually
        /// constructed multipart/form-data body.
        ///
        /// IMPORTANT — Bugzilla 7389 workaround:
        ///   https://bugzilla.proxmox.com/show_bug.cgi?id=7389
        ///
        ///   The Proxmox VE API rejects uploads when the multipart body contains
        ///   "Content-Type" or "Content-Transfer-Encoding" headers on the file part,
        ///   which is exactly what .NET's MultipartFormDataContent emits by default.
        ///   To work around this, this method constructs the raw multipart body manually:
        ///
        ///     * Text (form field) parts:  only Content-Disposition — no Content-Type,
        ///                                 no Content-Transfer-Encoding.
        ///     * File (binary) part:       Content-Disposition + Content-Type: application/octet-stream
        ///                                 — no Content-Transfer-Encoding.
        ///
        ///   Do NOT refactor this to use MultipartFormDataContent unless the PVE API
        ///   behaviour has been verified to have changed.
        /// </summary>
        /// <param name="resource">Relative API resource path, e.g. "/nodes/pve/storage/local/upload"</param>
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

            // Build a random 32-char alphanumeric boundary
            var boundary = GenerateBoundary();
            var fileName = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);
            var totalBytes = fileInfo.Length;

            // Build the multipart body as a MemoryStream / streaming approach.
            // We write preamble + form fields into a MemoryStream, then stream the
            // file content, then write the closing boundary — all via a custom
            // stream that concatenates them.

            var preamble = BuildMultipartPreamble(boundary, fileName, formFields, checksum, checksumAlgorithm);
            var closing = Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

            // Total upload size (preamble + file content + closing)
            var uploadSize = preamble.Length + totalBytes + closing.Length;

            var content = new PushStreamContent(async (outputStream) =>
            {
                // Write preamble
                await outputStream.WriteAsync(preamble, 0, preamble.Length).ConfigureAwait(false);

                // Stream file in 4 MB chunks, reporting progress
                const int chunkSize = 4 * 1024 * 1024;
                var buffer = new byte[chunkSize];
                long bytesSent = preamble.Length;

                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, chunkSize, useAsync: true))
                {
                    int bytesRead;
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        await outputStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                        bytesSent += bytesRead;
                        progressCallback?.Invoke(bytesSent, uploadSize);
                    }
                }

                // Write closing boundary
                await outputStream.WriteAsync(closing, 0, closing.Length).ConfigureAwait(false);
            }, uploadSize);

            content.Headers.ContentType = MediaTypeHeaderValue.Parse($"multipart/form-data; boundary=\"{boundary}\"");

            var request = BuildRequest(HttpMethod.Post, resource, mutating: true);
            request.Content = content;

            return await SendAsync(request, resource, "POST").ConfigureAwait(false);
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
                request.Headers.TryAddWithoutValidation("Authorization", $"PVEAPIToken={_session.ApiToken}");
            }
            else
            {
                // Ticket auth
                request.Headers.Add("Cookie", $"PVEAuthCookie={_session.Ticket}");
                if (mutating && !string.IsNullOrEmpty(_session.CsrfToken))
                    request.Headers.Add("CSRFPreventionToken", _session.CsrfToken);
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
                    return message;
            }
            catch
            {
                // If parsing fails, fall through to returning the raw body (truncated)
            }

            // Return raw body, truncated to avoid enormous exception messages
            return body.Length > 512 ? body.Substring(0, 512) + "..." : body;
        }

        /// <summary>Generates a random 32-character alphanumeric boundary string.</summary>
        private static string GenerateBoundary()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var rng = new Random();
            var sb = new StringBuilder(32);
            for (int i = 0; i < 32; i++)
                sb.Append(chars[rng.Next(chars.Length)]);
            return sb.ToString();
        }

        /// <summary>
        /// Builds the multipart preamble bytes: all form fields + the beginning of the file part
        /// (Content-Disposition + Content-Type only; no Content-Transfer-Encoding per bugzilla 7389).
        /// </summary>
        private static byte[] BuildMultipartPreamble(
            string boundary,
            string fileName,
            Dictionary<string, string>? formFields,
            string? checksum,
            string? checksumAlgorithm)
        {
            var sb = new StringBuilder();

            // Text form fields — Content-Disposition only, no Content-Type
            if (formFields != null)
            {
                foreach (var kvp in formFields)
                {
                    sb.Append($"--{boundary}\r\n");
                    sb.Append($"Content-Disposition: form-data; name=\"{kvp.Key}\"\r\n");
                    sb.Append("\r\n");
                    sb.Append(kvp.Value);
                    sb.Append("\r\n");
                }
            }

            if (!string.IsNullOrEmpty(checksum) && !string.IsNullOrEmpty(checksumAlgorithm))
            {
                sb.Append($"--{boundary}\r\n");
                sb.Append($"Content-Disposition: form-data; name=\"checksum-algorithm\"\r\n");
                sb.Append("\r\n");
                sb.Append(checksumAlgorithm);
                sb.Append("\r\n");

                sb.Append($"--{boundary}\r\n");
                sb.Append($"Content-Disposition: form-data; name=\"checksum\"\r\n");
                sb.Append("\r\n");
                sb.Append(checksum);
                sb.Append("\r\n");
            }
            else if (!string.IsNullOrEmpty(checksum))
            {
                sb.Append($"--{boundary}\r\n");
                sb.Append($"Content-Disposition: form-data; name=\"checksum\"\r\n");
                sb.Append("\r\n");
                sb.Append(checksum);
                sb.Append("\r\n");
            }

            // File part header — Content-Disposition + Content-Type; no Content-Transfer-Encoding
            sb.Append($"--{boundary}\r\n");
            sb.Append($"Content-Disposition: form-data; name=\"filename\"; filename=\"{fileName}\"\r\n");
            sb.Append("Content-Type: application/octet-stream\r\n");
            sb.Append("\r\n");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
        }

        // -------------------------------------------------------------------------
        // Helper: push-stream HttpContent for streaming uploads
        // -------------------------------------------------------------------------

        /// <summary>
        /// An <see cref="HttpContent"/> implementation that writes its body by invoking a
        /// caller-supplied async delegate, enabling streaming without buffering the entire
        /// payload into memory.
        /// </summary>
        private sealed class PushStreamContent : HttpContent
        {
            private readonly Func<Stream, Task> _onStreamAvailable;
            private readonly long _contentLength;

            public PushStreamContent(Func<Stream, Task> onStreamAvailable, long contentLength)
            {
                _onStreamAvailable = onStreamAvailable;
                _contentLength = contentLength;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                await _onStreamAvailable(stream).ConfigureAwait(false);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _contentLength;
                return _contentLength >= 0;
            }
        }
    }
}
