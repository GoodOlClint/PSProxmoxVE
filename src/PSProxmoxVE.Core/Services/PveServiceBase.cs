using System;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Common base for the API services: owns the optional injected <see cref="IPveHttpClient"/>
    /// and the client lifetime around each API operation. A service built without a client
    /// opens one per operation and disposes it afterwards; a service built with one uses it
    /// for every operation and never disposes it, since the caller owns its lifetime.
    /// </summary>
    public abstract class PveServiceBase
    {
        private readonly IPveHttpClient? _injectedClient;

        /// <summary>Initializes a service that opens its own HTTP client per call.</summary>
        private protected PveServiceBase() { }

        /// <summary>Initializes a service that uses the supplied HTTP client for every call.</summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        private protected PveServiceBase(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Runs <paramref name="action"/> against the injected client, or against a client
        /// opened for this call and disposed when it returns.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="action">The work to run against the client.</param>
        private protected T Invoke<T>(PveSession session, Func<IPveHttpClient, T> action) =>
            Invoke(session, timeoutOverride: null, action);

        /// <summary>
        /// Runs <paramref name="action"/> against the injected client, or against a client
        /// opened for this call and disposed when it returns.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="action">The work to run against the client.</param>
        private protected void Invoke(PveSession session, Action<IPveHttpClient> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Invoke<object?>(session, timeoutOverride: null, client => { action(client); return null; });
        }

        /// <summary>
        /// Runs <paramref name="action"/> against the injected client, or against a client
        /// opened for this call with <paramref name="timeoutOverride"/> applied and disposed
        /// when it returns. The override is ignored for an injected client.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="timeoutOverride">Per-call request timeout for a client opened here.</param>
        /// <param name="action">The work to run against the client.</param>
        private protected T Invoke<T>(PveSession session, TimeSpan? timeoutOverride, Func<IPveHttpClient, T> action)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (_injectedClient != null)
                return action(_injectedClient);

            var client = CreateClient(session, timeoutOverride);
            try
            {
                return action(client);
            }
            finally
            {
                client.Dispose();
            }
        }

        /// <summary>
        /// Test seam: builds the per-call client. Production services always get a
        /// <see cref="PveHttpClient"/> for <paramref name="session"/>.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="timeoutOverride">Per-call request timeout, or null for the session's.</param>
        internal virtual IPveHttpClient CreateClient(PveSession session, TimeSpan? timeoutOverride) =>
            new PveHttpClient(session, timeoutOverride);
    }
}
