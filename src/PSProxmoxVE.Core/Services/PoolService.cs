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
    public class PoolService : PveServiceBase
    {
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
        public PoolService(IPveHttpClient client) : base(client) { }

        /// <summary>
        /// Returns all resource pools.
        /// </summary>
        public PvePool[] GetPools(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("pools").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PvePool[]>() ?? Array.Empty<PvePool>();
            });
        }

        /// <summary>
        /// Returns a single resource pool by ID.
        /// </summary>
        public PvePool? GetPool(PveSession session, string poolId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"pools/{Uri.EscapeDataString(poolId)}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PvePool>();
            });
        }

        /// <summary>
        /// Creates a new resource pool.
        /// </summary>
        public void CreatePool(PveSession session, string poolId, string? comment = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));

            Invoke(session, client =>
            {
                var config = new Dictionary<string, string> { { "poolid", poolId } };
                if (!string.IsNullOrEmpty(comment))
                    config["comment"] = comment!;

                client.PostAsync("pools", config).GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Updates an existing resource pool.
        /// </summary>
        public void UpdatePool(PveSession session, string poolId, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Invoke(session, client =>
            {
                client.PutAsync($"pools/{Uri.EscapeDataString(poolId)}", config)
                    .GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Removes a resource pool.
        /// </summary>
        public void RemovePool(PveSession session, string poolId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));

            Invoke(session, client =>
            {
                client.DeleteAsync($"pools/{Uri.EscapeDataString(poolId)}")
                    .GetAwaiter().GetResult();
            });
        }
    }
}
