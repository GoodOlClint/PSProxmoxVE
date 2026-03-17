using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Network;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE node network and SDN (Software-Defined Networking) API operations.
    /// SDN methods require PVE 8.0 or later and will throw <see cref="PveVersionException"/> otherwise.
    /// </summary>
    public class NetworkService
    {
        // -------------------------------------------------------------------------
        // Node network interfaces
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns network interface configurations for a node.
        /// </summary>
        /// <param name="type">
        /// Optional interface type filter (e.g. "bridge", "bond", "eth", "vlan", "alias").
        /// </param>
        public PveNetwork[] GetNetworks(PveSession session, string node, string? type = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var resource = $"nodes/{node}/network";
            if (!string.IsNullOrEmpty(type))
                resource += $"?type={Uri.EscapeDataString(type!)}";

            using var client = new PveHttpClient(session);
            var response = client.GetAsync(resource).GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveNetwork[]>() ?? Array.Empty<PveNetwork>();
        }

        /// <summary>
        /// Creates a network interface on a node. Changes are pending until
        /// <see cref="ApplyNetworkConfig"/> is called. Returns the new interface config.
        /// </summary>
        public PveNetwork CreateNetwork(
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
            var response = client.PostAsync($"nodes/{node}/network", formData)
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveNetwork>() ?? new PveNetwork();
        }

        /// <summary>
        /// Updates an existing network interface on a node. Changes are pending until
        /// <see cref="ApplyNetworkConfig"/> is called.
        /// </summary>
        public void SetNetwork(
            PveSession session,
            string node,
            string iface,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(iface)) throw new ArgumentNullException(nameof(iface));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            var formData = config.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
            client.PutAsync($"nodes/{node}/network/{iface}", formData)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a network interface from a node. Changes are pending until
        /// <see cref="ApplyNetworkConfig"/> is called.
        /// </summary>
        public void RemoveNetwork(PveSession session, string node, string iface)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(iface)) throw new ArgumentNullException(nameof(iface));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"nodes/{node}/network/{iface}").GetAwaiter().GetResult();
        }

        /// <summary>
        /// Applies pending network configuration changes on a node. Returns the task UPID.
        /// </summary>
        public PveTask ApplyNetworkConfig(PveSession session, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);
            var response = client.PutAsync($"nodes/{node}/network")
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        // -------------------------------------------------------------------------
        // SDN — requires PVE 8.0+
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all SDN zone definitions. Requires PVE 8.0+.
        /// </summary>
        public PveSdnZone[] GetSdnZones(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            RequireSdn(session);

            using var client = new PveHttpClient(session);
            var response = client.GetAsync("cluster/sdn/zones").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnZone[]>() ?? Array.Empty<PveSdnZone>();
        }

        /// <summary>
        /// Returns all SDN VNet definitions. Requires PVE 8.0+.
        /// </summary>
        public PveSdnVnet[] GetSdnVnets(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            RequireSdn(session);

            using var client = new PveHttpClient(session);
            var response = client.GetAsync("cluster/sdn/vnets").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnVnet[]>() ?? Array.Empty<PveSdnVnet>();
        }

        /// <summary>
        /// Creates an SDN zone. Requires PVE 8.0+.
        /// </summary>
        public PveSdnZone CreateSdnZone(
            PveSession session,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            RequireSdn(session);
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            var formData = config.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
            var response = client.PostAsync("cluster/sdn/zones", formData)
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnZone>() ?? new PveSdnZone();
        }

        /// <summary>
        /// Removes an SDN zone. Requires PVE 8.0+.
        /// </summary>
        public void RemoveSdnZone(PveSession session, string zone)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            RequireSdn(session);
            if (string.IsNullOrWhiteSpace(zone)) throw new ArgumentNullException(nameof(zone));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"cluster/sdn/zones/{zone}").GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates an SDN VNet. Requires PVE 8.0+.
        /// </summary>
        public PveSdnVnet CreateSdnVnet(
            PveSession session,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            RequireSdn(session);
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            var formData = config.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
            var response = client.PostAsync("cluster/sdn/vnets", formData)
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnVnet>() ?? new PveSdnVnet();
        }

        /// <summary>
        /// Removes an SDN VNet. Requires PVE 8.0+.
        /// </summary>
        public void RemoveSdnVnet(PveSession session, string vnet)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            RequireSdn(session);
            if (string.IsNullOrWhiteSpace(vnet)) throw new ArgumentNullException(nameof(vnet));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"cluster/sdn/vnets/{vnet}").GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Throws <see cref="PveVersionException"/> if the session's server version is below 8.0.
        /// </summary>
        private static void RequireSdn(PveSession session)
        {
            if (session.ServerVersion != null && !session.ServerVersion.IsAtLeast(8, 0))
                throw new PveVersionException(8, 0, session.ServerVersion);
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
