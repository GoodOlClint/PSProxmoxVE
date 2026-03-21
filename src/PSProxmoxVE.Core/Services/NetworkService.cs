using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Network;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE node network and SDN (Software-Defined Networking) API operations.
    /// SDN methods require PVE 8.0 or later. Version checks are performed at the cmdlet layer.
    /// </summary>
    public class NetworkService
    {
        // -------------------------------------------------------------------------
        // Node network interfaces
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns network interface configurations for a node.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
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
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="config">Network interface configuration parameters.</param>
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
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="iface">The network interface name.</param>
        /// <param name="config">Network interface configuration parameters to update.</param>
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
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="iface">The network interface name to remove.</param>
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
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
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
        /// <param name="session">The authenticated PVE session.</param>
        public PveSdnZone[] GetSdnZones(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));


            using var client = new PveHttpClient(session);
            var response = client.GetAsync("cluster/sdn/zones").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnZone[]>() ?? Array.Empty<PveSdnZone>();
        }

        /// <summary>
        /// Returns all SDN VNet definitions. Requires PVE 8.0+.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        public PveSdnVnet[] GetSdnVnets(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));


            using var client = new PveHttpClient(session);
            var response = client.GetAsync("cluster/sdn/vnets").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnVnet[]>() ?? Array.Empty<PveSdnVnet>();
        }

        /// <summary>
        /// Creates an SDN zone. Requires PVE 8.0+.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="config">SDN zone configuration parameters.</param>
        public PveSdnZone CreateSdnZone(
            PveSession session,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

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
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="zone">The SDN zone identifier to remove.</param>
        public void RemoveSdnZone(PveSession session, string zone)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (string.IsNullOrWhiteSpace(zone)) throw new ArgumentNullException(nameof(zone));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"cluster/sdn/zones/{zone}").GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates an SDN VNet. Requires PVE 8.0+.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="config">SDN VNet configuration parameters.</param>
        public PveSdnVnet CreateSdnVnet(
            PveSession session,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

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
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="vnet">The SDN VNet identifier to remove.</param>
        public void RemoveSdnVnet(PveSession session, string vnet)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (string.IsNullOrWhiteSpace(vnet)) throw new ArgumentNullException(nameof(vnet));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"cluster/sdn/vnets/{vnet}").GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // SDN Subnets — requires PVE 8.0+
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all subnets for an SDN VNet. Requires PVE 8.0+.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="vnet">The SDN VNet identifier.</param>
        public PveSdnSubnet[] GetSdnSubnets(PveSession session, string vnet)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (string.IsNullOrWhiteSpace(vnet)) throw new ArgumentNullException(nameof(vnet));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"cluster/sdn/vnets/{Uri.EscapeDataString(vnet)}/subnets")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnSubnet[]>() ?? Array.Empty<PveSdnSubnet>();
        }

        /// <summary>
        /// Creates an SDN subnet on a VNet. Requires PVE 8.0+.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="vnet">The SDN VNet identifier.</param>
        /// <param name="config">Subnet configuration parameters including "subnet" (CIDR).</param>
        public void CreateSdnSubnet(
            PveSession session,
            string vnet,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (string.IsNullOrWhiteSpace(vnet)) throw new ArgumentNullException(nameof(vnet));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            var formData = config.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
            client.PostAsync($"cluster/sdn/vnets/{Uri.EscapeDataString(vnet)}/subnets", formData)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes an SDN subnet from a VNet. Requires PVE 8.0+.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="vnet">The SDN VNet identifier.</param>
        /// <param name="subnet">The subnet CIDR to remove.</param>
        public void RemoveSdnSubnet(PveSession session, string vnet, string subnet)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (string.IsNullOrWhiteSpace(vnet)) throw new ArgumentNullException(nameof(vnet));
            if (string.IsNullOrWhiteSpace(subnet)) throw new ArgumentNullException(nameof(subnet));

            using var client = new PveHttpClient(session);
            client.DeleteAsync(
                $"cluster/sdn/vnets/{Uri.EscapeDataString(vnet)}/subnets/{Uri.EscapeDataString(subnet)}")
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // SDN IPAM
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all SDN IPAM plugins. Requires PVE 8.0+.
        /// </summary>
        public PveSdnIpam[] GetSdnIpams(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));


            using var client = new PveHttpClient(session);
            var response = client.GetAsync("cluster/sdn/ipams").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnIpam[]>() ?? Array.Empty<PveSdnIpam>();
        }

        /// <summary>
        /// Creates an SDN IPAM plugin. Requires PVE 8.0+.
        /// </summary>
        public void CreateSdnIpam(PveSession session, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            client.PostAsync("cluster/sdn/ipams", config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes an SDN IPAM plugin. Requires PVE 8.0+.
        /// </summary>
        public void RemoveSdnIpam(PveSession session, string ipam)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (string.IsNullOrWhiteSpace(ipam)) throw new ArgumentNullException(nameof(ipam));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"cluster/sdn/ipams/{Uri.EscapeDataString(ipam)}")
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // SDN DNS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all SDN DNS plugins. Requires PVE 8.0+.
        /// </summary>
        public PveSdnDns[] GetSdnDnsPlugins(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));


            using var client = new PveHttpClient(session);
            var response = client.GetAsync("cluster/sdn/dns").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnDns[]>() ?? Array.Empty<PveSdnDns>();
        }

        /// <summary>
        /// Creates an SDN DNS plugin. Requires PVE 8.0+.
        /// </summary>
        public void CreateSdnDnsPlugin(PveSession session, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            client.PostAsync("cluster/sdn/dns", config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes an SDN DNS plugin. Requires PVE 8.0+.
        /// </summary>
        public void RemoveSdnDnsPlugin(PveSession session, string dns)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (string.IsNullOrWhiteSpace(dns)) throw new ArgumentNullException(nameof(dns));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"cluster/sdn/dns/{Uri.EscapeDataString(dns)}")
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // SDN Controllers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all SDN controllers. Requires PVE 8.0+.
        /// </summary>
        public PveSdnController[] GetSdnControllers(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));


            using var client = new PveHttpClient(session);
            var response = client.GetAsync("cluster/sdn/controllers").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveSdnController[]>() ?? Array.Empty<PveSdnController>();
        }

        /// <summary>
        /// Creates an SDN controller. Requires PVE 8.0+.
        /// </summary>
        public void CreateSdnController(PveSession session, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            client.PostAsync("cluster/sdn/controllers", config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes an SDN controller. Requires PVE 8.0+.
        /// </summary>
        public void RemoveSdnController(PveSession session, string controller)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (string.IsNullOrWhiteSpace(controller)) throw new ArgumentNullException(nameof(controller));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"cluster/sdn/controllers/{Uri.EscapeDataString(controller)}")
                .GetAwaiter().GetResult();
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
