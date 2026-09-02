using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace PSProxmoxVE.Core.Client
{
    /// <summary>
    /// Process-wide cache of <see cref="HttpClientHandler"/> instances, one per
    /// (host, port, skipCertificateCheck). The handler owns the connection pool, so sharing
    /// it lets every <see cref="PveHttpClient"/> for the same endpoint reuse established TLS
    /// connections instead of handshaking per request. Cached handlers are never disposed
    /// and are immutable once published: never set a property on a handler this returns.
    /// </summary>
    internal sealed class PveHandlerCache
    {
        /// <summary>The cache production clients share.</summary>
        internal static readonly PveHandlerCache Shared = new PveHandlerCache(CreateHandler);

        private readonly object _gate = new object();
        private readonly Dictionary<string, HttpClientHandler> _handlers =
            new Dictionary<string, HttpClientHandler>(StringComparer.OrdinalIgnoreCase);
        private readonly Func<bool, HttpClientHandler> _factory;

        /// <summary>
        /// Test seam: a cache whose handlers come from <paramref name="factory"/>, which
        /// receives the skipCertificateCheck flag for the key being populated.
        /// </summary>
        internal PveHandlerCache(Func<bool, HttpClientHandler> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>Number of handlers built so far.</summary>
        internal int Count
        {
            get { lock (_gate) return _handlers.Count; }
        }

        /// <summary>Returns the handler for the endpoint, building it on first use.</summary>
        internal HttpClientHandler Get(string host, int port, bool skipCertificateCheck)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host cannot be null or empty.", nameof(host));

            var key = host + ":" + port + ":" + (skipCertificateCheck ? "insecure" : "verify");
            lock (_gate)
            {
                if (!_handlers.TryGetValue(key, out var handler))
                {
                    handler = _factory(skipCertificateCheck);
                    _handlers[key] = handler;
                }
                return handler;
            }
        }

        private static HttpClientHandler CreateHandler(bool skipCertificateCheck)
        {
            var handler = new HttpClientHandler { UseCookies = false };
            if (skipCertificateCheck)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (HttpRequestMessage _, X509Certificate2 _, X509Chain _, SslPolicyErrors _) => true;
            }
            return handler;
        }
    }
}
