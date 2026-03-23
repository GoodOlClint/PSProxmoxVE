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
        private readonly IPveHttpClient? _injectedClient;
        private readonly NodeService _nodeService = new NodeService();

        /// <summary>Initializes a new instance that creates its own HTTP clients.</summary>
        public ContainerService() { }

        /// <summary>Initializes a new instance that uses the supplied HTTP client for all requests.</summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        public ContainerService(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

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
                catch (Exception ex) when (ex is PSProxmoxVE.Core.Exceptions.PveApiException or System.Net.Http.HttpRequestException)
                {
                    // Skip offline/inaccessible nodes
                }
            }
            return all.ToArray();
        }

        private PveContainer[] GetContainersOnNode(PveSession session, string node)
        {
            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/lxc").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveContainer[]>() ?? Array.Empty<PveContainer>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/config")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveContainerConfig>() ?? new PveContainerConfig();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/config", formData)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/snapshot")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSnapshot[]>() ?? Array.Empty<PveSnapshot>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/snapshot", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}/rollback")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/status/shutdown", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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
            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}{purgeParam}")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/clone", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/migrate", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Suspend / Resume
        // -------------------------------------------------------------------------

        /// <summary>Suspends a container. Returns the task UPID.</summary>
        public PveTask SuspendContainer(PveSession session, string node, int vmid)
            => PostStatus(session, node, vmid, "suspend");

        /// <summary>Resumes a suspended container. Returns the task UPID.</summary>
        public PveTask ResumeContainer(PveSession session, string node, int vmid)
            => PostStatus(session, node, vmid, "resume");

        // -------------------------------------------------------------------------
        // Disk / volume operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Resizes a container disk/volume. Returns the task UPID.
        /// </summary>
        public PveTask ResizeContainerDisk(PveSession session, string node, int vmid, string disk, string size)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(disk)) throw new ArgumentNullException(nameof(disk));
            if (string.IsNullOrWhiteSpace(size)) throw new ArgumentNullException(nameof(size));

            var formData = new Dictionary<string, string>
            {
                ["disk"] = disk,
                ["size"] = size
            };

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/resize", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Moves a container volume to a different storage. Returns the task UPID.
        /// </summary>
        public PveTask MoveVolume(PveSession session, string node, int vmid, string volume, string storage, bool delete = true)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(volume)) throw new ArgumentNullException(nameof(volume));
            if (string.IsNullOrWhiteSpace(storage)) throw new ArgumentNullException(nameof(storage));

            var formData = new Dictionary<string, string>
            {
                ["volume"] = volume,
                ["storage"] = storage,
                ["delete"] = delete ? "1" : "0"
            };

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/move_volume", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Template
        // -------------------------------------------------------------------------

        /// <summary>
        /// Converts a container to a template. This is irreversible. Returns the task UPID.
        /// </summary>
        public PveTask ConvertToTemplate(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/template")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Interfaces
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns network interface information for a container.
        /// </summary>
        public PveContainerInterface[] GetInterfaces(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/interfaces")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveContainerInterface[]>() ?? Array.Empty<PveContainerInterface>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private PveTask PostStatus(PveSession session, string node, int vmid, string action)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/status/{action}")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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
