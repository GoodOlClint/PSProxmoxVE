using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Nodes;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for node-level and cluster-version Proxmox VE API operations.
    /// </summary>
    public class NodeService
    {
        /// <summary>
        /// Returns all cluster nodes.
        /// </summary>
        public PveNode[] GetNodes(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync("nodes").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveNode[]>() ?? Array.Empty<PveNode>();
        }

        /// <summary>
        /// Returns detailed status for a specific node.
        /// </summary>
        public PveNodeStatus GetNodeStatus(PveSession session, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"nodes/{node}/status").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveNodeStatus>() ?? new PveNodeStatus();
        }

        /// <summary>
        /// Returns the Proxmox VE version running on the server.
        /// </summary>
        public PveVersion GetVersion(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync("version").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            var versionStr = data?["version"]?.ToString();
            if (string.IsNullOrEmpty(versionStr))
                throw new InvalidOperationException("Failed to retrieve PVE version from API response.");
            return PveVersion.Parse(versionStr);
        }
    }
}
