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
        /// <summary>
        /// Returns the current cluster status. The response is a mixed array of
        /// "cluster" and "node" type entries.
        /// </summary>
        public PveClusterStatus[] GetClusterStatus(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync("cluster/status").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveClusterStatus[]>() ?? Array.Empty<PveClusterStatus>();
        }
    }
}
