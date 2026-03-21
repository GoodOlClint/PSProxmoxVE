using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Nodes;
using PSProxmoxVE.Core.Models.Vms;

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
        /// Returns the configuration of a specific node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        public JObject GetNodeConfig(PveSession session, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"nodes/{node}/config").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data as JObject ?? new JObject();
        }

        /// <summary>
        /// Updates the configuration of a specific node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="config">Configuration parameters to update.</param>
        public void SetNodeConfig(PveSession session, string node, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            client.PutAsync($"nodes/{node}/config", config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns the DNS configuration of a specific node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        public JObject GetNodeDns(PveSession session, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"nodes/{node}/dns").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data as JObject ?? new JObject();
        }

        /// <summary>
        /// Updates the DNS configuration of a specific node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="config">DNS configuration parameters to update.</param>
        public void SetNodeDns(PveSession session, string node, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            client.PutAsync($"nodes/{node}/dns", config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Starts all VMs and containers on a node. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="config">Optional parameters (e.g. vms to limit which VMs start).</param>
        public PveTask StartAll(PveSession session, string node, Dictionary<string, string>? config = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var formData = config ?? new Dictionary<string, string>();
            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/startall", formData).GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>
        /// Stops all VMs and containers on a node. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="config">Optional parameters (e.g. vms, force-stop).</param>
        public PveTask StopAll(PveSession session, string node, Dictionary<string, string>? config = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var formData = config ?? new Dictionary<string, string>();
            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/stopall", formData).GetAwaiter().GetResult();
            return ParseTask(response, node);
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
            return PveVersion.Parse(versionStr!);
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private static PveTask ParseTask(string response, string node)
        {
            var data = JObject.Parse(response)["data"];
            if (data?.Type == JTokenType.String)
                return new PveTask { Upid = data.ToString(), Node = node };

            var task = data?.ToObject<PveTask>() ?? new PveTask();
            task.Node = node;
            return task;
        }
    }
}
