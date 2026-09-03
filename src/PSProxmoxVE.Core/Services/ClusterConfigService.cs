using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Cluster;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE cluster configuration API operations
    /// (/cluster/config, /cluster/options, /cluster/nextid).
    /// </summary>
    public class ClusterConfigService : PveServiceBase
    {
        private static readonly TimeSpan DefaultQuorumTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan QuorumPollInterval = TimeSpan.FromSeconds(2);

        private readonly ClusterService _clusterService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterConfigService"/> class.
        /// </summary>
        public ClusterConfigService()
        {
            _clusterService = new ClusterService();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterConfigService"/> class with an injected HTTP client.
        /// </summary>
        /// <param name="client">The HTTP client to use for API calls. The caller owns its lifetime.</param>
        public ClusterConfigService(IPveHttpClient client) : base(client)
        {
            _clusterService = new ClusterService(client);
        }

        /// <summary>
        /// Returns the cluster configuration directory (GET /cluster/config).
        /// The response is a mixed structure returned as a Dictionary.
        /// </summary>
        public Dictionary<string, object?> GetClusterConfig(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("cluster/config").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data is JObject obj ? JsonHelper.ToDictionary(obj) : new Dictionary<string, object?>();
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync("cluster/config", data).GetAwaiter().GetResult();
                var result = JObject.Parse(response)["data"];
                return result?.ToString() ?? string.Empty;
            });
        }

        /// <summary>
        /// Returns the list of nodes in the cluster configuration (GET /cluster/config/nodes).
        /// </summary>
        public PveClusterConfigNode[] GetConfigNodes(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("cluster/config/nodes").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveClusterConfigNode[]>() ?? Array.Empty<PveClusterConfigNode>();
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"cluster/config/nodes/{Uri.EscapeDataString(node)}", data).GetAwaiter().GetResult();
                var result = JObject.Parse(response)["data"];
                return result?.ToString() ?? string.Empty;
            });
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

            Invoke(session, client =>
            {
                client.DeleteAsync($"cluster/config/nodes/{Uri.EscapeDataString(node)}").GetAwaiter().GetResult();
            });
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

            return Invoke(session, client =>
            {
                var response = client.GetAsync(resource).GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveClusterJoinInfo>() ?? new PveClusterJoinInfo();
            });
        }

        /// <summary>
        /// Joins the current node to an existing cluster (POST /cluster/config/join).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="hostname">The hostname or IP of an existing cluster node.</param>
        /// <param name="fingerprint">The TLS certificate fingerprint of the cluster node.</param>
        /// <param name="password">The root password for the cluster node (plain string; cmdlet layer handles SecureString conversion per ADR 0002).</param>
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync("cluster/config/join", data).GetAwaiter().GetResult();
                var result = JObject.Parse(response)["data"];
                return result?.ToString() ?? string.Empty;
            });
        }

        /// <summary>
        /// Returns the cluster-wide options (GET /cluster/options).
        /// </summary>
        public PveClusterOptions GetClusterOptions(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("cluster/options").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveClusterOptions>() ?? new PveClusterOptions();
            });
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

            Invoke(session, client =>
            {
                client.PutAsync("cluster/options", options).GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Blocks until the cluster reports quorum (GET /cluster/status, quorate = 1).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="timeout">Maximum time to wait. Defaults to 60 seconds.</param>
        /// <remarks>
        /// The cluster-create task completes before corosync converges; until the node
        /// is quorate it rejects joins with "cluster not ready - no quorum?". API errors
        /// during that window are transient and are retried until the deadline.
        /// </remarks>
        /// <exception cref="TimeoutException">Quorum was not reached before the deadline.</exception>
        public void WaitForQuorum(PveSession session, TimeSpan? timeout = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var effectiveTimeout = timeout ?? DefaultQuorumTimeout;
            var deadline = DateTime.UtcNow.Add(effectiveTimeout);

            while (true)
            {
                try
                {
                    foreach (var entry in _clusterService.GetClusterStatus(session))
                    {
                        if (string.Equals(entry.Type, "cluster", StringComparison.OrdinalIgnoreCase)
                            && entry.Quorate == 1)
                            return;
                    }
                }
                catch (PveApiException)
                {
                    // pmxcfs and corosync restart while the cluster forms.
                }

                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException(
                        $"Cluster did not reach quorum within {effectiveTimeout.TotalSeconds:0} seconds.");

                Thread.Sleep(QuorumPollInterval);
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

            return Invoke(session, client =>
            {
                var response = client.GetAsync(resource).GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                if (data == null)
                    throw new InvalidOperationException("API response for next VMID did not contain a 'data' field.");
                if (int.TryParse(data.ToString(), out var id))
                    return id;
                throw new InvalidOperationException($"API returned unexpected next VMID value: '{data}'");
            });
        }
    }
}
