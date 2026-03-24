using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Nodes;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE QEMU/KVM virtual machine API operations.
    /// </summary>
    public class VmService
    {
        private readonly IPveHttpClient? _injectedClient;
        private readonly NodeService _nodeService = new NodeService();

        /// <summary>Initializes a new instance that creates its own HTTP clients.</summary>
        public VmService() { }

        /// <summary>Initializes a new instance that uses the supplied HTTP client for all requests.</summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        public VmService(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        // -------------------------------------------------------------------------
        // Read operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns VMs. If <paramref name="node"/> is null, queries every cluster node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">Optional cluster node name to filter VMs by node.</param>
        public PveVm[] GetVms(PveSession session, string? node = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (node != null)
                return GetVmsOnNode(session, node);

            // Query all nodes and aggregate
            var nodes = _nodeService.GetNodes(session);
            var all = new List<PveVm>();
            foreach (var n in nodes)
            {
                try
                {
                    var vms = GetVmsOnNode(session, n.Name);
                    // Stamp the node name in case it wasn't returned by the API
                    foreach (var vm in vms)
                        vm.Node ??= n.Name;
                    all.AddRange(vms);
                }
                catch (Exception ex) when (ex is PSProxmoxVE.Core.Exceptions.PveApiException or System.Net.Http.HttpRequestException)
                {
                    // Skip nodes that are offline or inaccessible
                }
            }
            return all.ToArray();
        }

        private PveVm[] GetVmsOnNode(PveSession session, string node)
        {
            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveVm[]>() ?? Array.Empty<PveVm>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns a single VM by its ID on the specified node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveVm GetVm(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var vms = GetVmsOnNode(session, node);
            var vm = vms.FirstOrDefault(v => v.VmId == vmid);
            if (vm == null)
                throw new InvalidOperationException($"VM {vmid} not found on node '{node}'.");
            vm.Node ??= node;
            return vm;
        }

        /// <summary>
        /// Enriches a VM object with detailed status from the status/current endpoint,
        /// populating QmpStatus and other fields not available from the list endpoint.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vm">The VM to enrich.</param>
        public void EnrichVmStatus(PveSession session, string node, PveVm vm)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (vm == null) throw new ArgumentNullException(nameof(vm));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vm.VmId}/status/current")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                if (data == null) return;

                vm.QmpStatus = data["qmpstatus"]?.ToString();
                vm.Status = data["status"]?.ToString() ?? vm.Status;
                vm.Pid = data["pid"]?.ToObject<int?>();
                vm.Uptime = data["uptime"]?.ToObject<long?>();
                vm.CpuCount = data["cpus"]?.ToObject<int?>() ?? vm.CpuCount;
                vm.MaxMem = data["maxmem"]?.ToObject<long?>() ?? vm.MaxMem;
                vm.MaxDisk = data["maxdisk"]?.ToObject<long?>() ?? vm.MaxDisk;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the full configuration of a VM.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveVmConfig GetVmConfig(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/config")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveVmConfig>() ?? new PveVmConfig();
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
        /// Updates one or more VM configuration settings. Changes are applied immediately (POST).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="config">VM configuration parameters to update.</param>
        public void SetVmConfig(PveSession session, string node, int vmid, Dictionary<string, object> config)
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
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/config", formData)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Disk import
        // -------------------------------------------------------------------------

        /// <summary>
        /// Imports a disk image into a VM by setting a disk config key with the import-from syntax.
        /// Uses POST (not PUT) because the import is an async background operation.
        /// Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="disk">The disk key (e.g. "scsi0", "sata0", "virtio0").</param>
        /// <param name="targetStorage">The target storage for the imported disk (e.g. "local-lvm").</param>
        /// <param name="importFrom">
        /// The import source in PVE format. Examples:
        /// <list type="bullet">
        /// <item>"local:iso/image.img" — import from a file already on storage</item>
        /// <item>"local:import/myvm.ova/disk.vmdk" — import a disk from within an OVA</item>
        /// <item>"/var/lib/vz/images/disk.qcow2" — import from an absolute path on the node</item>
        /// </list>
        /// </param>
        /// <param name="format">Optional target format (e.g. "qcow2", "raw"). Defaults to storage default.</param>
        public PveTask ImportDisk(
            PveSession session,
            string node,
            int vmid,
            string disk,
            string targetStorage,
            string importFrom,
            string? format = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(disk)) throw new ArgumentNullException(nameof(disk));
            if (string.IsNullOrWhiteSpace(targetStorage)) throw new ArgumentNullException(nameof(targetStorage));
            if (string.IsNullOrWhiteSpace(importFrom)) throw new ArgumentNullException(nameof(importFrom));

            // Build the disk value: "storage:0,import-from=source[,format=fmt]"
            var diskValue = $"{targetStorage}:0,import-from={importFrom}";
            if (!string.IsNullOrEmpty(format))
                diskValue += $",format={format}";

            var formData = new Dictionary<string, string>
            {
                [disk] = diskValue
            };

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                // POST (not PUT) because import-from triggers a background task
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/config", formData)
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
        /// Creates a new VM. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="config">VM configuration parameters.</param>
        public PveTask CreateVm(PveSession session, string node, Dictionary<string, object> config)
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
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>Starts a VM. Returns the task UPID.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveTask StartVm(PveSession session, string node, int vmid)
            => PostStatus(session, node, vmid, "start");

        /// <summary>Stops a VM (hard power-off). Returns the task UPID.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveTask StopVm(PveSession session, string node, int vmid)
            => PostStatus(session, node, vmid, "stop");

        /// <summary>Gracefully shuts down a VM. Returns the task UPID.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="timeoutSeconds">Optional shutdown timeout in seconds.</param>
        public PveTask ShutdownVm(PveSession session, string node, int vmid, int? timeoutSeconds = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var formData = new Dictionary<string, string>();
            if (timeoutSeconds.HasValue)
                formData["timeout"] = timeoutSeconds.Value.ToString();

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/status/shutdown", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>Resets a VM (hard reset). Returns the task UPID.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveTask ResetVm(PveSession session, string node, int vmid)
            => PostStatus(session, node, vmid, "reset");

        /// <summary>Suspends a VM (writes RAM state to disk). Returns the task UPID.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveTask SuspendVm(PveSession session, string node, int vmid)
            => PostStatus(session, node, vmid, "suspend");

        /// <summary>Resumes a suspended VM. Returns the task UPID.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveTask ResumeVm(PveSession session, string node, int vmid)
            => PostStatus(session, node, vmid, "resume");

        // -------------------------------------------------------------------------
        // Removal / migration / clone
        // -------------------------------------------------------------------------

        /// <summary>
        /// Removes a VM. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="purge">If true, also removes all associated backup files and jobs.</param>
        public PveTask RemoveVm(PveSession session, string node, int vmid, bool purge = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var purgeParam = purge ? "?purge=1" : "?purge=0";
            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}{purgeParam}")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Clones a VM. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The source VM ID to clone.</param>
        /// <param name="newid">The VM ID for the new clone.</param>
        /// <param name="name">Optional name for the cloned VM.</param>
        /// <param name="targetNode">Optional target node for the clone.</param>
        /// <param name="full">If true, creates a full clone; otherwise a linked clone.</param>
        public PveTask CloneVm(
            PveSession session,
            string node,
            int vmid,
            int newid,
            string? name = null,
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
            if (!string.IsNullOrEmpty(name)) formData["name"] = name!;
            if (!string.IsNullOrEmpty(targetNode)) formData["target"] = targetNode!;

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/clone", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Migrates a VM to another node. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The source cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="targetNode">The target node to migrate to.</param>
        /// <param name="online">If true, performs an online (live) migration.</param>
        public PveTask MigrateVm(
            PveSession session,
            string node,
            int vmid,
            string targetNode,
            bool online = true)
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
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/migrate", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Resizes a disk attached to a VM. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="disk">Disk identifier, e.g. "scsi0" or "virtio0".</param>
        /// <param name="size">
        /// New absolute size (e.g. "32G") or relative increase with "+" prefix (e.g. "+10G").
        /// </param>
        public PveTask ResizeDisk(
            PveSession session,
            string node,
            int vmid,
            string disk,
            string size)
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
                var response = client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/resize", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
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
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/status/{action}")
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
            // Many endpoints return the UPID string directly as the data value
            if (data?.Type == JTokenType.String)
                return new PveTask { Upid = data.ToString(), Node = node };

            var task = data?.ToObject<PveTask>() ?? new PveTask();
            task.Node = node;
            return task;
        }

        // -------------------------------------------------------------------------
        // QEMU Guest Agent
        // -------------------------------------------------------------------------

        /// <summary>
        /// Pings the QEMU guest agent on the specified VM. Returns true if responsive.
        /// </summary>
        public bool PingGuestAgent(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/ping").GetAwaiter().GetResult();
                return true;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                return false;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Retrieves network interface information from the QEMU guest agent.
        /// </summary>
        public PveGuestNetworkInterface[] GetGuestNetworkInterfaces(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/network-get-interfaces")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var result = data?["result"];
                return result?.ToObject<PveGuestNetworkInterface[]>() ?? Array.Empty<PveGuestNetworkInterface>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Executes a command inside the guest via the QEMU guest agent.
        /// Returns the PID of the spawned process.
        /// </summary>
        public int ExecuteGuestCommand(PveSession session, string node, int vmid,
            string command, string[]? args = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(command)) throw new ArgumentNullException(nameof(command));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var data = new Dictionary<string, string>
                {
                    ["command"] = command
                };

                if (args != null && args.Length > 0)
                {
                    // PVE expects input-data for arguments passed as a JSON-encoded string array
                    var argsJson = Newtonsoft.Json.JsonConvert.SerializeObject(args);
                    data["input-data"] = argsJson;
                }

                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/exec", data)
                    .GetAwaiter().GetResult();
                var pid = JObject.Parse(response)["data"]?["pid"]?.ToObject<int>() ?? 0;
                return pid;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Gets the status/result of a guest agent exec command by PID.
        /// </summary>
        public JObject GetGuestExecStatus(PveSession session, string node, int vmid, int pid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/exec-status?pid={pid}")
                    .GetAwaiter().GetResult();
                return JObject.Parse(response)["data"] as JObject ?? new JObject();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Disk operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Moves a VM disk to a different storage. Returns the task UPID.
        /// </summary>
        public PveTask MoveDisk(PveSession session, string node, int vmid, string disk, string storage, string? format = null, bool delete = true)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(disk)) throw new ArgumentNullException(nameof(disk));
            if (string.IsNullOrWhiteSpace(storage)) throw new ArgumentNullException(nameof(storage));

            var formData = new Dictionary<string, string>
            {
                ["disk"] = disk,
                ["storage"] = storage,
                ["delete"] = delete ? "1" : "0"
            };
            if (!string.IsNullOrEmpty(format))
                formData["format"] = format!;

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/move_disk", formData)
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Unlinks (detaches) disks from a VM.
        /// </summary>
        public void UnlinkDisk(PveSession session, string node, int vmid, string idlist, bool force = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(idlist)) throw new ArgumentNullException(nameof(idlist));

            var formData = new Dictionary<string, string>
            {
                ["idlist"] = idlist
            };
            if (force)
                formData["force"] = "1";

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/unlink", formData)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Guest agent — extended operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Retrieves OS information from the QEMU guest agent.
        /// </summary>
        public PveGuestOsInfo? GetGuestOsInfo(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/get-osinfo")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var result = data?["result"];
                return result?.ToObject<PveGuestOsInfo>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Retrieves filesystem information from the QEMU guest agent.
        /// </summary>
        public PveGuestFsInfo[] GetGuestFsInfo(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/get-fsinfo")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var result = data?["result"];
                return result?.ToObject<PveGuestFsInfo[]>() ?? Array.Empty<PveGuestFsInfo>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Reads a file from the guest filesystem via the QEMU guest agent.
        /// </summary>
        public string ReadGuestFile(PveSession session, string node, int vmid, string file)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(file)) throw new ArgumentNullException(nameof(file));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/file-read?file={Uri.EscapeDataString(file)}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?["content"]?.ToString() ?? string.Empty;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Writes content to a file on the guest filesystem via the QEMU guest agent.
        /// </summary>
        public void WriteGuestFile(PveSession session, string node, int vmid, string file, string content)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(file)) throw new ArgumentNullException(nameof(file));

            var formData = new Dictionary<string, string>
            {
                ["file"] = file,
                ["content"] = content
            };

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/file-write", formData)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Sets a user password inside the guest via the QEMU guest agent.
        /// </summary>
        public void SetGuestPassword(PveSession session, string node, int vmid, string username, string password, bool crypted = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException(nameof(username));

            var formData = new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password
            };
            if (crypted)
                formData["crypted"] = "1";

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/set-user-password", formData)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Triggers an fstrim operation inside the guest via the QEMU guest agent.
        /// </summary>
        public void GuestFsTrim(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/fstrim")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // OVA Upload
        // -------------------------------------------------------------------------

        /// <summary>
        /// Uploads an OVA file to storage with content=import. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="storage">The target storage identifier.</param>
        /// <param name="ovaPath">The local path to the OVA file.</param>
        /// <param name="progressCallback">
        /// Optional callback invoked periodically with (bytesSent, totalBytes).
        /// May be called from a background thread.
        /// </param>
        public PveTask UploadOva(
            PveSession session,
            string node,
            string storage,
            string ovaPath,
            Action<long, long>? progressCallback = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(storage)) throw new ArgumentNullException(nameof(storage));
            if (string.IsNullOrWhiteSpace(ovaPath)) throw new ArgumentNullException(nameof(ovaPath));

            var formFields = new Dictionary<string, string>
            {
                ["content"] = "import"
            };

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.UploadFileAsync(
                        $"nodes/{Uri.EscapeDataString(node)}/storage/{Uri.EscapeDataString(storage)}/upload",
                        ovaPath,
                        formFields,
                        progressCallback: progressCallback)
                    .GetAwaiter().GetResult();

                var root = JObject.Parse(response);
                var upid = root["data"]?.ToString() ?? string.Empty;
                return new PveTask { Upid = upid, Node = node, Status = "running" };
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }
    }
}
