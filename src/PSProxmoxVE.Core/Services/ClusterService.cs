using System;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Cluster;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE cluster-level API operations.
    /// </summary>
    public class ClusterService
    {
        private readonly IPveHttpClient? _injectedClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterService"/> class.
        /// </summary>
        public ClusterService() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterService"/> class with an injected HTTP client.
        /// </summary>
        /// <param name="client">The HTTP client to use for API calls. The caller owns its lifetime.</param>
        public ClusterService(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Returns the current cluster status. The response is a mixed array of
        /// "cluster" and "node" type entries.
        /// </summary>
        public PveClusterStatus[] GetClusterStatus(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/status").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveClusterStatus[]>() ?? Array.Empty<PveClusterStatus>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns cluster resources, optionally filtered by type.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="type">Optional resource type filter (vm, lxc, node, storage, sdn).</param>
        public PveClusterResource[] GetClusterResources(PveSession session, string? type = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var resource = "cluster/resources";
                if (!string.IsNullOrEmpty(type))
                    resource += $"?type={Uri.EscapeDataString(type!)}";

                var response = client.GetAsync(resource).GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveClusterResource[]>() ?? Array.Empty<PveClusterResource>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }
    }
}
