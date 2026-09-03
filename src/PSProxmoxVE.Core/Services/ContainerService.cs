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
    public class ContainerService : PveServiceBase
    {
        private readonly NodeService _nodeService;

        /// <summary>Initializes a new instance that creates its own HTTP clients.</summary>
        public ContainerService()
        {
            _nodeService = new NodeService();
        }

        /// <summary>Initializes a new instance that uses the supplied HTTP client for all requests.</summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        public ContainerService(IPveHttpClient client) : base(client)
        {
            _nodeService = new NodeService(client);
        }

        // -------------------------------------------------------------------------
        // Read operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns containers. If <paramref name="node"/> is null, queries every cluster node.
        /// </summary>
        /// <param name="onNodeSkipped">
        /// Optional callback invoked with the node name and the exception when a node is
        /// skipped because it is unreachable (connectivity failure or a 5xx from that node).
        /// A 401/403/404 or any other non-5xx <see cref="PSProxmoxVE.Core.Exceptions.PveApiException"/>
        /// propagates instead of being swallowed.
        /// </param>
        public PveContainer[] GetContainers(PveSession session, string? node = null, Action<string, Exception>? onNodeSkipped = null)
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
                catch (Exception ex) when (IsNodeUnreachable(ex))
                {
                    onNodeSkipped?.Invoke(n.Name, ex);
                }
            }
            return all.ToArray();
        }

        /// <summary>
        /// True for a connectivity failure or a 5xx PVE API response — the cases where the
        /// node itself is unreachable rather than the request being rejected. A 401/403/404
        /// (or any other non-5xx status) means the request was understood and refused, which
        /// is not something a per-node listing loop should hide.
        /// </summary>
        private static bool IsNodeUnreachable(Exception ex) => ex switch
        {
            System.Net.Http.HttpRequestException => true,
            // PveHttpClient wraps a connection failure as 503 and a client-side timeout as
            // 408 (Client/PveHttpClient.cs SendOnceAsync) — both mean the node did not answer,
            // not that it rejected the request.
            PSProxmoxVE.Core.Exceptions.PveApiException apiEx =>
                apiEx.StatusCode == System.Net.HttpStatusCode.RequestTimeout || (int)apiEx.StatusCode >= 500,
            _ => false
        };

        private PveContainer[] GetContainersOnNode(PveSession session, string node)
        {
            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/lxc").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveContainer[]>() ?? Array.Empty<PveContainer>();
            });
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

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/config")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveContainerConfig>() ?? new PveContainerConfig();
            });
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

            Invoke(session, client =>
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/config", formData)
                    .GetAwaiter().GetResult();
            });
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

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/snapshot")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSnapshot[]>() ?? Array.Empty<PveSnapshot>();
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/snapshot", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}/rollback")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
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

            return Invoke(session, client =>
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/status/shutdown", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
        }

        /// <summary>Removes a container. Returns the task UPID.</summary>
        public PveTask RemoveContainer(
            PveSession session,
            string node,
            int vmid,
            bool purge = false,
            bool force = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var queryParams = new List<string>();
            queryParams.Add(purge ? "purge=1" : "purge=0");
            if (force)
                queryParams.Add("force=1");
            var queryString = "?" + string.Join("&", queryParams);

            return Invoke(session, client =>
            {
                var response = client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}{queryString}")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
        }

        /// <summary>Clones a container. Returns the task UPID.</summary>
        public PveTask CloneContainer(
            PveSession session,
            string node,
            int vmid,
            int newid,
            string? hostname = null,
            string? targetNode = null,
            bool full = true,
            string? storage = null)
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
            if (!string.IsNullOrEmpty(storage)) formData["storage"] = storage!;

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/clone", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/migrate", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/resize", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/move_volume", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/template")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/interfaces")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveContainerInterface[]>() ?? Array.Empty<PveContainerInterface>();
            });
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private PveTask PostStatus(PveSession session, string node, int vmid, string action)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid}/status/{action}")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            });
        }

        private static PveTask ParseTask(string response, string node)
        {
            var data = JObject.Parse(response)["data"];
            if (data?.Type == JTokenType.String)
                return new PveTask { Upid = data.ToString(), Node = node, Status = "running" };

            var task = data?.ToObject<PveTask>() ?? new PveTask();
            task.Node = node;
            return task;
        }
    }
}
