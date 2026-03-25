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
    public class NodeService
    {
        private readonly IPveHttpClient? _injectedClient;

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
        public NodeService(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Returns all cluster nodes.
        /// </summary>
        public PveNode[] GetNodes(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("nodes").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveNode[]>() ?? Array.Empty<PveNode>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns detailed status for a specific node.
        /// </summary>
        public PveNodeStatus GetNodeStatus(PveSession session, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/status").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveNodeStatus>() ?? new PveNodeStatus();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/config").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return JsonHelper.ToDictionary(data as JObject);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/config", config).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/dns").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return JsonHelper.ToDictionary(data as JObject);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/dns", config).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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
            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/startall", formData).GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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
            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/stopall", formData).GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the Proxmox VE version running on the server.
        /// </summary>
        public PveVersion GetVersion(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("version").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var versionStr = data?["version"]?.ToString();
                if (string.IsNullOrEmpty(versionStr))
                    throw new InvalidOperationException("Failed to retrieve PVE version from API response.");
                return PveVersion.Parse(versionStr!);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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
