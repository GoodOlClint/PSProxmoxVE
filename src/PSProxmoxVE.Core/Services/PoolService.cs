using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Cluster;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE resource pool API operations.
    /// </summary>
    public class PoolService
    {
        private readonly IPveHttpClient? _injectedClient;

        /// <summary>
        /// Initializes a new instance of <see cref="PoolService"/> with no injected client.
        /// Each method will create and dispose its own <see cref="PveHttpClient"/>.
        /// </summary>
        public PoolService() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PoolService"/> with an injected HTTP client.
        /// The caller owns the client's lifetime; this service will not dispose it.
        /// </summary>
        /// <param name="client">The HTTP client to use for all requests.</param>
        public PoolService(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Returns all resource pools.
        /// </summary>
        public PvePool[] GetPools(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("pools").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PvePool[]>() ?? Array.Empty<PvePool>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns a single resource pool by ID.
        /// </summary>
        public PvePool? GetPool(PveSession session, string poolId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"pools/{Uri.EscapeDataString(poolId)}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PvePool>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Creates a new resource pool.
        /// </summary>
        public void CreatePool(PveSession session, string poolId, string? comment = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var config = new Dictionary<string, string> { { "poolid", poolId } };
                if (!string.IsNullOrEmpty(comment))
                    config["comment"] = comment!;

                client.PostAsync("pools", config).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an existing resource pool.
        /// </summary>
        public void UpdatePool(PveSession session, string poolId, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"pools/{Uri.EscapeDataString(poolId)}", config)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Removes a resource pool.
        /// </summary>
        public void RemovePool(PveSession session, string poolId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"pools/{Uri.EscapeDataString(poolId)}")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }
    }
}
