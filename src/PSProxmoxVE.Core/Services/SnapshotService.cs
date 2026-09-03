using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE VM snapshot API operations.
    /// All operations apply to QEMU/KVM VMs via the /nodes/{node}/qemu/{vmid}/snapshot endpoints.
    /// </summary>
    public class SnapshotService : PveServiceBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotService"/> class.
        /// </summary>
        public SnapshotService() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotService"/> class with an injected HTTP client.
        /// </summary>
        /// <param name="client">The HTTP client to use for API calls. The caller owns its lifetime.</param>
        public SnapshotService(IPveHttpClient client) : base(client) { }

        /// <summary>
        /// Returns all snapshots for a VM.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveSnapshot[] GetSnapshots(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSnapshot[]>() ?? Array.Empty<PveSnapshot>();
            });
        }

        /// <summary>
        /// Creates a snapshot of a VM. Returns the task PVE started.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="snapname">Snapshot name (alphanumeric, no spaces).</param>
        /// <param name="description">Optional description.</param>
        /// <param name="vmstate">Whether to save VM RAM state (live snapshot). Sent only when true.</param>
        public PveTask CreateSnapshot(
            PveSession session,
            string node,
            int vmid,
            string snapname,
            string? description = null,
            bool vmstate = false)
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
            if (vmstate)
                formData["vmstate"] = "1";

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot", formData)
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
        }

        /// <summary>
        /// Removes a snapshot from a VM. Returns the task PVE started.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="snapname">The snapshot name to remove.</param>
        public PveTask RemoveSnapshot(
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
                var response = client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}")
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
        }

        /// <summary>
        /// Rolls a VM back to a snapshot. Returns the task PVE started.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="snapname">The snapshot name to roll back to.</param>
        public PveTask RollbackSnapshot(
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
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}/rollback")
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
        }
    }
}
