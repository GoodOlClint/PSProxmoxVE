using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Nodes;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for node-level and cluster-version Proxmox VE API operations.
    /// </summary>
    public class NodeService : PveServiceBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NodeService"/> with no injected client.
        /// Each method will create and dispose its own <see cref="PveHttpClient"/>.
        /// </summary>
        public NodeService() { }

        /// <summary>
        /// Initializes a new instance of <see cref="NodeService"/> with an injected HTTP client.
        /// The caller owns the client's lifetime; this service will not dispose it.
        /// </summary>
        /// <param name="client">The HTTP client to use for all requests.</param>
        public NodeService(IPveHttpClient client) : base(client) { }

        /// <summary>
        /// Returns all cluster nodes.
        /// </summary>
        public PveNode[] GetNodes(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("nodes").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"]
                    ?? throw new InvalidOperationException("Response did not contain a 'data' field.");
                return data.ToObject<PveNode[]>() ?? Array.Empty<PveNode>();
            });
        }

        /// <summary>
        /// Returns detailed status for a specific node.
        /// </summary>
        public PveNodeStatus GetNodeStatus(PveSession session, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/status").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"]
                    ?? throw new InvalidOperationException("Response did not contain a 'data' field.");
                var status = data.ToObject<PveNodeStatus>()
                    ?? throw new InvalidOperationException("Failed to deserialize node status.");
                // The /nodes/{node}/status response does not include the node name.
                if (string.IsNullOrEmpty(status.Node))
                    status.Node = node;
                return status;
            });
        }

        /// <summary>
        /// Returns the configuration of a specific node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        public Dictionary<string, object?> GetNodeConfig(PveSession session, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/config").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return JsonHelper.ToDictionary(data as JObject);
            });
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

            Invoke(session, client =>
            {
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/config", config).GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Returns the DNS configuration of a specific node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        public Dictionary<string, object?> GetNodeDns(PveSession session, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/dns").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return JsonHelper.ToDictionary(data as JObject);
            });
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

            Invoke(session, client =>
            {
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/dns", config).GetAwaiter().GetResult();
            });
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
            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/startall", formData).GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
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
            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/stopall", formData).GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
        }
    }
}
