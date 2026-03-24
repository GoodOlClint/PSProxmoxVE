using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Cluster;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE cluster configuration API operations
    /// (/cluster/config, /cluster/options, /cluster/nextid).
    /// </summary>
    public class ClusterConfigService
    {
        private readonly IPveHttpClient? _injectedClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterConfigService"/> class.
        /// </summary>
        public ClusterConfigService() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterConfigService"/> class with an injected HTTP client.
        /// </summary>
        /// <param name="client">The HTTP client to use for API calls. The caller owns its lifetime.</param>
        public ClusterConfigService(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Returns the cluster configuration directory (GET /cluster/config).
        /// The response is a mixed structure returned as a raw JObject.
        /// </summary>
        public JObject GetClusterConfig(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/config").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data as JObject ?? new JObject();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Creates a new cluster (POST /cluster/config).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="clusterName">The name for the new cluster.</param>
        /// <param name="links">Optional Corosync link addresses, using keys link0..link7 (e.g., "link0=10.0.0.1").</param>
        /// <param name="nodeid">Optional node ID for this node.</param>
        /// <param name="votes">Optional number of quorum votes for this node.</param>
        /// <returns>The UPID of the cluster creation task.</returns>
        public string CreateCluster(PveSession session, string clusterName, Dictionary<string, string>? links = null, int? nodeid = null, int? votes = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrEmpty(clusterName)) throw new ArgumentNullException(nameof(clusterName));

            var data = new Dictionary<string, string>
            {
                ["clustername"] = clusterName
            };
            if (links != null)
            {
                foreach (var kvp in links)
                    data[kvp.Key] = kvp.Value;
            }
            if (nodeid.HasValue)
                data["nodeid"] = nodeid.Value.ToString();
            if (votes.HasValue)
                data["votes"] = votes.Value.ToString();

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync("cluster/config", data).GetAwaiter().GetResult();
                var result = JObject.Parse(response)["data"];
                return result?.ToString() ?? string.Empty;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the list of nodes in the cluster configuration (GET /cluster/config/nodes).
        /// </summary>
        public PveClusterConfigNode[] GetConfigNodes(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/config/nodes").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveClusterConfigNode[]>() ?? Array.Empty<PveClusterConfigNode>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Adds a node to the cluster configuration (POST /cluster/config/nodes/{node}).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The node name to add.</param>
        /// <param name="newNodeIp">The IP address of the new node.</param>
        /// <param name="links">Optional Corosync link addresses.</param>
        /// <param name="nodeid">Optional node ID for the new node.</param>
        /// <param name="votes">Optional number of quorum votes.</param>
        /// <param name="force">Optional flag to force the operation.</param>
        /// <param name="apiversion">Optional API version override.</param>
        /// <returns>The UPID of the add-node task.</returns>
        public string AddConfigNode(PveSession session, string node, string? newNodeIp = null, Dictionary<string, string>? links = null, int? nodeid = null, int? votes = null, bool? force = null, int? apiversion = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrEmpty(node)) throw new ArgumentNullException(nameof(node));

            var data = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(newNodeIp))
                data["new_node_ip"] = newNodeIp!;
            if (links != null)
            {
                foreach (var kvp in links)
                    data[kvp.Key] = kvp.Value;
            }
            if (nodeid.HasValue)
                data["nodeid"] = nodeid.Value.ToString();
            if (votes.HasValue)
                data["votes"] = votes.Value.ToString();
            if (force.HasValue)
                data["force"] = force.Value ? "1" : "0";
            if (apiversion.HasValue)
                data["apiversion"] = apiversion.Value.ToString();

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"cluster/config/nodes/{Uri.EscapeDataString(node)}", data).GetAwaiter().GetResult();
                var result = JObject.Parse(response)["data"];
                return result?.ToString() ?? string.Empty;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Removes a node from the cluster configuration (DELETE /cluster/config/nodes/{node}).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The node name to remove.</param>
        public void RemoveConfigNode(PveSession session, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrEmpty(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"cluster/config/nodes/{Uri.EscapeDataString(node)}").GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the cluster join information (GET /cluster/config/join).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">Optional node name to get join info for a specific node.</param>
        public PveClusterJoinInfo GetJoinInfo(PveSession session, string? node = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var resource = "cluster/config/join";
            if (!string.IsNullOrEmpty(node))
                resource += $"?node={Uri.EscapeDataString(node!)}";

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync(resource).GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveClusterJoinInfo>() ?? new PveClusterJoinInfo();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Joins the current node to an existing cluster (POST /cluster/config/join).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="hostname">The hostname or IP of an existing cluster node.</param>
        /// <param name="fingerprint">The TLS certificate fingerprint of the cluster node.</param>
        /// <param name="password">The root password for the cluster node (plain string; cmdlet layer handles SecureString conversion per D002).</param>
        /// <param name="links">Optional Corosync link addresses.</param>
        /// <param name="nodeid">Optional node ID for this node.</param>
        /// <param name="votes">Optional number of quorum votes.</param>
        /// <param name="force">Optional flag to force the join.</param>
        /// <returns>The UPID of the join task.</returns>
        public string JoinCluster(PveSession session, string hostname, string fingerprint, string password, Dictionary<string, string>? links = null, int? nodeid = null, int? votes = null, bool? force = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrEmpty(hostname)) throw new ArgumentNullException(nameof(hostname));
            if (string.IsNullOrEmpty(fingerprint)) throw new ArgumentNullException(nameof(fingerprint));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            var data = new Dictionary<string, string>
            {
                ["hostname"] = hostname,
                ["fingerprint"] = fingerprint,
                ["password"] = password
            };
            if (links != null)
            {
                foreach (var kvp in links)
                    data[kvp.Key] = kvp.Value;
            }
            if (nodeid.HasValue)
                data["nodeid"] = nodeid.Value.ToString();
            if (votes.HasValue)
                data["votes"] = votes.Value.ToString();
            if (force.HasValue)
                data["force"] = force.Value ? "1" : "0";

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync("cluster/config/join", data).GetAwaiter().GetResult();
                var result = JObject.Parse(response)["data"];
                return result?.ToString() ?? string.Empty;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the Corosync totem configuration (GET /cluster/config/totem).
        /// </summary>
        public JObject GetTotem(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/config/totem").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data as JObject ?? new JObject();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the external quorum device (qdevice) status (GET /cluster/config/qdevice).
        /// </summary>
        public JObject GetQdevice(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/config/qdevice").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data as JObject ?? new JObject();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the cluster API version (GET /cluster/config/apiversion).
        /// </summary>
        public int GetApiVersion(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/config/apiversion").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<int>() ?? 0;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the cluster-wide options (GET /cluster/options).
        /// </summary>
        public PveClusterOptions GetClusterOptions(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/options").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveClusterOptions>() ?? new PveClusterOptions();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Sets cluster-wide options (PUT /cluster/options).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="options">A dictionary of option names and values to set.</param>
        public void SetClusterOptions(PveSession session, Dictionary<string, string> options)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync("cluster/options", options).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the current cluster status (GET /cluster/status).
        /// Delegates to the same endpoint as <see cref="ClusterService.GetClusterStatus"/>.
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
        /// Returns the next available VM/CT ID (GET /cluster/nextid).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="vmid">Optional specific VMID to check availability for.</param>
        /// <returns>The next available VMID as an integer.</returns>
        public int GetNextId(PveSession session, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var resource = "cluster/nextid";
            if (vmid.HasValue)
                resource += $"?vmid={vmid.Value}";

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync(resource).GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                // The API returns the ID as a string, parse it to int
                if (data != null && int.TryParse(data.ToString(), out var id))
                    return id;
                return 0;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }
    }
}
