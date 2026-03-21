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
        /// <summary>
        /// Returns all resource pools.
        /// </summary>
        public PvePool[] GetPools(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync("pools").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PvePool[]>() ?? Array.Empty<PvePool>();
        }

        /// <summary>
        /// Returns a single resource pool by ID.
        /// </summary>
        public PvePool? GetPool(PveSession session, string poolId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"pools/{Uri.EscapeDataString(poolId)}")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PvePool>();
        }

        /// <summary>
        /// Creates a new resource pool.
        /// </summary>
        public void CreatePool(PveSession session, string poolId, string? comment = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));

            using var client = new PveHttpClient(session);
            var config = new Dictionary<string, string> { { "poolid", poolId } };
            if (!string.IsNullOrEmpty(comment))
                config["comment"] = comment!;

            client.PostAsync("pools", config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates an existing resource pool.
        /// </summary>
        public void UpdatePool(PveSession session, string poolId, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            client.PutAsync($"pools/{Uri.EscapeDataString(poolId)}", config)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a resource pool.
        /// </summary>
        public void RemovePool(PveSession session, string poolId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(poolId)) throw new ArgumentNullException(nameof(poolId));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"pools/{Uri.EscapeDataString(poolId)}")
                .GetAwaiter().GetResult();
        }
    }
}
