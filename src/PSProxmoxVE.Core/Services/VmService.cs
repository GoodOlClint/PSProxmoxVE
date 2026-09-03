using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE QEMU/KVM virtual machine API operations.
    /// </summary>
    public class VmService : PveServiceBase
    {
        /// <summary>Initializes a new instance that creates its own HTTP clients.</summary>
        public VmService()
        {
        }

        /// <summary>Initializes a new instance that uses the supplied HTTP client for all requests.</summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        public VmService(IPveHttpClient client) : base(client)
        {
        }

        // -------------------------------------------------------------------------
        // Read operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns VMs. If <paramref name="node"/> is null, lists the whole cluster with a
        /// single call to <c>cluster/resources?type=vm</c> instead of one call per node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">Optional cluster node name to filter VMs by node.</param>
        /// <param name="onNodeSkipped">
        /// Not invoked. The cluster-wide listing is one call to <c>cluster/resources</c>
        /// with no per-node failure to report; kept on the signature for source
        /// compatibility with existing callers.
        /// </param>
        public PveVm[] GetVms(PveSession session, string? node = null, Action<string, Exception>? onNodeSkipped = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (node != null)
                return GetVmsOnNode(session, node);

            return Invoke(session, client =>
            {
                const string resource = "cluster/resources?type=vm";
                var response = client.GetAsync(resource).GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var resources = data?.ToObject<PSProxmoxVE.Core.Models.Cluster.PveClusterResource[]>()
                    ?? Array.Empty<PSProxmoxVE.Core.Models.Cluster.PveClusterResource>();
                // "type=vm" is PVE's guest filter, not a QEMU-only one — it returns both
                // "qemu" and "lxc" rows, so the QEMU guests need filtering out here.
                return resources.Where(r => r.Type == "qemu").Select(ToPveVm).ToArray();
            });
        }

        /// <summary>
        /// Maps a <c>/cluster/resources</c> row (type "qemu") onto <see cref="PveVm"/>. The
        /// resources endpoint does not carry <see cref="PveVm.QmpStatus"/>,
        /// <see cref="PveVm.Pid"/> or <see cref="PveVm.AgentStatus"/> — those remain null
        /// until <c>Get-PveVm -Detailed</c> or <see cref="GetVm"/> enrich from
        /// <c>status/current</c>.
        /// </summary>
        private static PveVm ToPveVm(PSProxmoxVE.Core.Models.Cluster.PveClusterResource r) => new PveVm
        {
            VmId = r.VmId ?? 0,
            Name = r.Name,
            Status = r.Status,
            Node = r.Node,
            CpuCount = r.MaxCpu.HasValue ? (int)r.MaxCpu.Value : (int?)null,
            MaxMem = r.MaxMem,
            MaxDisk = r.MaxDisk,
            Uptime = r.Uptime,
            Tags = r.Tags,
            Template = r.Template ?? 0,
            Lock = r.Lock,
        };

        private PveVm[] GetVmsOnNode(PveSession session, string node)
        {
            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveVm[]>() ?? Array.Empty<PveVm>();
            });
        }

        /// <summary>
        /// Returns a single VM by its ID, fetched directly from
        /// <c>status/current</c> rather than listing the whole node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveVm GetVm(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                JToken? data;
                try
                {
                    var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/status/current")
                        .GetAwaiter().GetResult();
                    data = JObject.Parse(response)["data"];
                }
                catch (PSProxmoxVE.Core.Exceptions.PveApiException ex) when (
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
                    ex.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    // Preserves the not-found contract GetVmsOnNode gave callers (e.g.
                    // ImportPveOvaCmdlet) when the VM wasn't in the node's listing yet.
                    // Excludes 502/503/504 deliberately: PveHttpClient.SendOnceAsync wraps a
                    // connectivity failure as 503, which is the node being unreachable, not
                    // the VM being absent, and must propagate rather than read as not-found.
                    throw new InvalidOperationException($"VM {vmid} not found on node '{node}'.", ex);
                }

                if (data == null)
                    throw new InvalidOperationException($"VM {vmid} not found on node '{node}'.");

                var vm = data.ToObject<PveVm>() ?? new PveVm { VmId = vmid };
                vm.Node ??= node;
                return vm;
            });
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

            Invoke(session, client =>
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
            });
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

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/config")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveVmConfig>() ?? new PveVmConfig();
            });
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

            Invoke(session, client =>
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/config", formData)
                    .GetAwaiter().GetResult();
            });
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

            return Invoke(session, client =>
            {
                // POST (not PUT) because import-from triggers a background task
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/config", formData)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
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

            return Invoke(session, client =>
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu", formData)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/status/shutdown", formData)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
        }

        /// <summary>
        /// Reboots a VM through PVE's native reboot endpoint. Returns the task UPID.
        /// </summary>
        /// <remarks>
        /// PVE holds the guest's config lock across the whole shutdown and restarts the VM from
        /// its own post-stop cleanup, so nothing can interleave between the two halves. Composing
        /// a reboot client-side as shutdown + start instead races that cleanup: the start wins the
        /// lock, cleanup then holds it for 30 s waiting on the newly started process, and the next
        /// call fails with "can't lock file '/var/lock/qemu-server/lock-&lt;vmid&gt;.conf' - got timeout".
        /// </remarks>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="timeoutSeconds">Optional maximum seconds to wait for the shutdown half.</param>
        public PveTask RebootVm(PveSession session, string node, int vmid, int? timeoutSeconds = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var formData = new Dictionary<string, string>();
            if (timeoutSeconds.HasValue)
                formData["timeout"] = timeoutSeconds.Value.ToString();

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/status/reboot", formData)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
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
        /// <param name="skipLock">If true, bypasses locks (PVE honours this for root@pam only).</param>
        public PveTask RemoveVm(PveSession session, string node, int vmid, bool purge = false, bool skipLock = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var queryParams = new List<string>();
            queryParams.Add(purge ? "purge=1" : "purge=0");
            if (skipLock)
                queryParams.Add("skiplock=1");
            var queryString = "?" + string.Join("&", queryParams);

            return Invoke(session, client =>
            {
                var response = client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}{queryString}")
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
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
            if (!string.IsNullOrEmpty(name)) formData["name"] = name!;
            if (!string.IsNullOrEmpty(targetNode)) formData["target"] = targetNode!;
            if (!string.IsNullOrEmpty(storage)) formData["storage"] = storage!;

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/clone", formData)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/migrate", formData)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/resize", formData)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
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
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/status/{action}")
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
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

            return Invoke(session, client =>
            {
                try
                {
                    client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/ping").GetAwaiter().GetResult();
                    return true;
                }
                catch (PSProxmoxVE.Core.Exceptions.PveApiException)
                {
                    // Treat API-level failures for this endpoint as "guest agent not responding".
                    return false;
                }
            });
        }

        /// <summary>
        /// Retrieves network interface information from the QEMU guest agent.
        /// </summary>
        public PveGuestNetworkInterface[] GetGuestNetworkInterfaces(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/network-get-interfaces")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var result = data?["result"];
                return result?.ToObject<PveGuestNetworkInterface[]>() ?? Array.Empty<PveGuestNetworkInterface>();
            });
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

            return Invoke(session, client =>
            {
                // PVE's agent/exec "command" is an array: element 0 is the executable and
                // each subsequent element is one argv entry. It is sent as repeated form
                // keys (command=<exe>&command=<arg1>&...). Do NOT use "input-data" for
                // arguments — that is the process's STDIN, not argv.
                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("command", command)
                };
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        if (arg == null)
                            throw new ArgumentException("Args elements must not be null.", nameof(args));
                        data.Add(new KeyValuePair<string, string>("command", arg));
                    }
                }

                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/exec", data)
                    .GetAwaiter().GetResult();
                var pid = JObject.Parse(response)["data"]?["pid"]?.ToObject<int>() ?? 0;
                return pid;
            });
        }

        /// <summary>
        /// Gets the status/result of a guest agent exec command by PID.
        /// </summary>
        public Dictionary<string, object?> GetGuestExecStatus(PveSession session, string node, int vmid, int pid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/exec-status?pid={pid}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return JsonHelper.ToDictionary(data as JObject);
            });
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

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/move_disk", formData)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
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

            Invoke(session, client =>
            {
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/unlink", formData)
                    .GetAwaiter().GetResult();
            });
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

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/get-osinfo")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var result = data?["result"];
                return result?.ToObject<PveGuestOsInfo>();
            });
        }

        /// <summary>
        /// Retrieves filesystem information from the QEMU guest agent.
        /// </summary>
        public PveGuestFsInfo[] GetGuestFsInfo(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/get-fsinfo")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var result = data?["result"];
                return result?.ToObject<PveGuestFsInfo[]>() ?? Array.Empty<PveGuestFsInfo>();
            });
        }

        /// <summary>
        /// Reads a file from the guest filesystem via the QEMU guest agent.
        /// </summary>
        public string ReadGuestFile(PveSession session, string node, int vmid, string file)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(file)) throw new ArgumentNullException(nameof(file));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/file-read?file={Uri.EscapeDataString(file)}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?["content"]?.ToString() ?? string.Empty;
            });
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

            Invoke(session, client =>
            {
                client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/file-write", formData)
                    .GetAwaiter().GetResult();
            });
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

            Invoke(session, client =>
            {
                client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/set-user-password", formData)
                    .GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Triggers an fstrim operation inside the guest via the QEMU guest agent.
        /// </summary>
        public void GuestFsTrim(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            Invoke(session, client =>
            {
                client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/agent/fstrim")
                    .GetAwaiter().GetResult();
            });
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
        /// <param name="timeout">
        /// HTTP timeout override for this upload. Defaults to 30 minutes, overriding the
        /// session's default 100-second timeout so that large OVA files have time to transfer.
        /// </param>
        public PveTask UploadOva(
            PveSession session,
            string node,
            string storage,
            string ovaPath,
            Action<long, long>? progressCallback = null,
            TimeSpan? timeout = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(storage)) throw new ArgumentNullException(nameof(storage));
            if (string.IsNullOrWhiteSpace(ovaPath)) throw new ArgumentNullException(nameof(ovaPath));

            var formFields = new Dictionary<string, string>
            {
                ["content"] = "import"
            };

            return Invoke(session, timeout ?? TimeSpan.FromMinutes(30), client =>
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
            });
        }
    }
}
