using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE VM snapshot API operations.
    /// All operations apply to QEMU/KVM VMs via the /nodes/{node}/qemu/{vmid}/snapshot endpoints.
    /// </summary>
    public class SnapshotService
    {
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

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSnapshot[]>() ?? Array.Empty<PveSnapshot>();
        }

        /// <summary>
        /// Creates a snapshot of a VM. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="snapname">Snapshot name (alphanumeric, no spaces).</param>
        /// <param name="description">Optional description.</param>
        /// <param name="vmstate">Whether to save VM RAM state (live snapshot). Default false.</param>
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
                ["snapname"] = snapname,
                ["vmstate"] = vmstate ? "1" : "0"
            };
            if (!string.IsNullOrEmpty(description))
                formData["description"] = description!;

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot", formData)
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>
        /// Removes a snapshot from a VM. Returns the task UPID.
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

            using var client = new PveHttpClient(session);
            var response = client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}")
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>
        /// Rolls a VM back to a snapshot. Returns the task UPID.
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

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot/{Uri.EscapeDataString(snapname)}/rollback")
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
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
