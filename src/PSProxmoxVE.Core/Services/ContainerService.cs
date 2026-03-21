using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Containers;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE Linux Container (LXC) API operations.
    /// </summary>
    public class ContainerService
    {
        private readonly NodeService _nodeService = new NodeService();

        // -------------------------------------------------------------------------
        // Read operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns containers. If <paramref name="node"/> is null, queries every cluster node.
        /// </summary>
        public PveContainer[] GetContainers(PveSession session, string? node = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (node != null)
                return GetContainersOnNode(session, node);

            var nodes = _nodeService.GetNodes(session);
            var all = new List<PveContainer>();
            foreach (var n in nodes)
            {
                try
                {
                    var containers = GetContainersOnNode(session, n.Name);
                    foreach (var ct in containers)
                        ct.Node ??= n.Name;
                    all.AddRange(containers);
                }
                catch
                {
                    // Skip offline/inaccessible nodes
                }
            }
            return all.ToArray();
        }

        private PveContainer[] GetContainersOnNode(PveSession session, string node)
        {
            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"nodes/{node}/lxc").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveContainer[]>() ?? Array.Empty<PveContainer>();
        }

        /// <summary>
        /// Returns a single container by its ID on the specified node.
        /// </summary>
        public PveContainer GetContainer(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var containers = GetContainersOnNode(session, node);
            var ct = containers.FirstOrDefault(c => c.VmId == vmid);
            if (ct == null)
                throw new InvalidOperationException($"Container {vmid} not found on node '{node}'.");
            ct.Node ??= node;
            return ct;
        }

        /// <summary>
        /// Returns the full configuration of a container.
        /// </summary>
        public PveContainerConfig GetContainerConfig(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"nodes/{node}/lxc/{vmid}/config")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveContainerConfig>() ?? new PveContainerConfig();
        }

        // -------------------------------------------------------------------------
        // Configuration mutation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Updates one or more container configuration settings.
        /// </summary>
        public void SetContainerConfig(
            PveSession session,
            string node,
            int vmid,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            var formData = config.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
            client.PutAsync($"nodes/{node}/lxc/{vmid}/config", formData)
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // Snapshots
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all snapshots for a container.
        /// </summary>
        public PveSnapshot[] GetContainerSnapshots(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"nodes/{node}/lxc/{vmid}/snapshot")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSnapshot[]>() ?? Array.Empty<PveSnapshot>();
        }

        /// <summary>
        /// Creates a snapshot of a container. Returns the task UPID.
        /// </summary>
        public PveTask CreateContainerSnapshot(
            PveSession session,
            string node,
            int vmid,
            string snapname,
            string? description = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(snapname)) throw new ArgumentNullException(nameof(snapname));

            var formData = new Dictionary<string, string>
            {
                ["snapname"] = snapname
            };
            if (!string.IsNullOrEmpty(description))
                formData["description"] = description!;

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/lxc/{vmid}/snapshot", formData)
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>
        /// Removes a snapshot from a container. Returns the task UPID.
        /// </summary>
        public PveTask RemoveContainerSnapshot(
            PveSession session,
            string node,
            int vmid,
            string snapname)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(snapname)) throw new ArgumentNullException(nameof(snapname));

            using var client = new PveHttpClient(session);
            var response = client.DeleteAsync($"nodes/{node}/lxc/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}")
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>
        /// Rolls a container back to a snapshot. Returns the task UPID.
        /// </summary>
        public PveTask RollbackContainerSnapshot(
            PveSession session,
            string node,
            int vmid,
            string snapname)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(snapname)) throw new ArgumentNullException(nameof(snapname));

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/lxc/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}/rollback")
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        /// <summary>
        /// Creates a new container. Returns the task UPID.
        /// </summary>
        public PveTask CreateContainer(
            PveSession session,
            string node,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            var formData = config.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
            var response = client.PostAsync($"nodes/{node}/lxc", formData)
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>Starts a container. Returns the task UPID.</summary>
        public PveTask StartContainer(PveSession session, string node, int vmid)
            => PostStatus(session, node, vmid, "start");

        /// <summary>Stops a container (hard stop). Returns the task UPID.</summary>
        public PveTask StopContainer(PveSession session, string node, int vmid)
            => PostStatus(session, node, vmid, "stop");

        /// <summary>Gracefully shuts down a container. Returns the task UPID.</summary>
        public PveTask ShutdownContainer(
            PveSession session,
            string node,
            int vmid,
            int? timeoutSeconds = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var formData = new Dictionary<string, string>();
            if (timeoutSeconds.HasValue)
                formData["timeout"] = timeoutSeconds.Value.ToString();

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/lxc/{vmid}/status/shutdown", formData)
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>Removes a container. Returns the task UPID.</summary>
        public PveTask RemoveContainer(
            PveSession session,
            string node,
            int vmid,
            bool purge = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var purgeParam = purge ? "?purge=1" : "?purge=0";
            using var client = new PveHttpClient(session);
            var response = client.DeleteAsync($"nodes/{node}/lxc/{vmid}{purgeParam}")
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>Clones a container. Returns the task UPID.</summary>
        public PveTask CloneContainer(
            PveSession session,
            string node,
            int vmid,
            int newid,
            string? hostname = null,
            string? targetNode = null,
            bool full = true)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var formData = new Dictionary<string, string>
            {
                ["newid"] = newid.ToString(),
                ["full"] = full ? "1" : "0"
            };
            if (!string.IsNullOrEmpty(hostname)) formData["hostname"] = hostname!;
            if (!string.IsNullOrEmpty(targetNode)) formData["target"] = targetNode!;

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/lxc/{vmid}/clone", formData)
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>Migrates a container to another node. Returns the task UPID.</summary>
        public PveTask MigrateContainer(
            PveSession session,
            string node,
            int vmid,
            string targetNode,
            bool online = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(targetNode)) throw new ArgumentNullException(nameof(targetNode));

            var formData = new Dictionary<string, string>
            {
                ["target"] = targetNode,
                ["online"] = online ? "1" : "0"
            };

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/lxc/{vmid}/migrate", formData)
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private PveTask PostStatus(PveSession session, string node, int vmid, string action)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/lxc/{vmid}/status/{action}")
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

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
