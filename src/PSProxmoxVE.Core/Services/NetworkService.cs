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
        private readonly IPveHttpClient? _injectedClient;

        /// <summary>Initializes a new instance that creates its own HTTP clients.</summary>
        public NetworkService() { }

        /// <summary>Initializes a new instance that uses the supplied HTTP client for all requests.</summary>
        /// <param name="client">The HTTP client to use. The caller owns its lifetime.</param>
        public NetworkService(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

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

            var resource = $"nodes/{Uri.EscapeDataString(node)}/network";
            if (!string.IsNullOrEmpty(type))
                resource += $"?type={Uri.EscapeDataString(type!)}";

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync(resource).GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveNetwork[]>() ?? Array.Empty<PveNetwork>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/network", formData)
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveNetwork>() ?? new PveNetwork();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/network/{Uri.EscapeDataString(iface)}", formData)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"nodes/{Uri.EscapeDataString(node)}/network/{Uri.EscapeDataString(iface)}").GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/network")
                    .GetAwaiter().GetResult();
                return ParseTask(response, node);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/sdn/zones").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSdnZone[]>() ?? Array.Empty<PveSdnZone>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns all SDN VNet definitions. Requires PVE 8.0+.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        public PveSdnVnet[] GetSdnVnets(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/sdn/vnets").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSdnVnet[]>() ?? Array.Empty<PveSdnVnet>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                var response = client.PostAsync("cluster/sdn/zones", formData)
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSdnZone>() ?? new PveSdnZone();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"cluster/sdn/zones/{Uri.EscapeDataString(zone)}").GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                var response = client.PostAsync("cluster/sdn/vnets", formData)
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSdnVnet>() ?? new PveSdnVnet();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"cluster/sdn/vnets/{Uri.EscapeDataString(vnet)}").GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"cluster/sdn/vnets/{Uri.EscapeDataString(vnet)}/subnets")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSdnSubnet[]>() ?? Array.Empty<PveSdnSubnet>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                client.PostAsync($"cluster/sdn/vnets/{Uri.EscapeDataString(vnet)}/subnets", formData)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync(
                    $"cluster/sdn/vnets/{Uri.EscapeDataString(vnet)}/subnets/{Uri.EscapeDataString(subnet)}")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/sdn/ipams").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSdnIpam[]>() ?? Array.Empty<PveSdnIpam>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Creates an SDN IPAM plugin. Requires PVE 8.0+.
        /// </summary>
        public void CreateSdnIpam(PveSession session, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync("cluster/sdn/ipams", config).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Removes an SDN IPAM plugin. Requires PVE 8.0+.
        /// </summary>
        public void RemoveSdnIpam(PveSession session, string ipam)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(ipam)) throw new ArgumentNullException(nameof(ipam));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"cluster/sdn/ipams/{Uri.EscapeDataString(ipam)}")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/sdn/dns").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSdnDns[]>() ?? Array.Empty<PveSdnDns>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Creates an SDN DNS plugin. Requires PVE 8.0+.
        /// </summary>
        public void CreateSdnDnsPlugin(PveSession session, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync("cluster/sdn/dns", config).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Removes an SDN DNS plugin. Requires PVE 8.0+.
        /// </summary>
        public void RemoveSdnDnsPlugin(PveSession session, string dns)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(dns)) throw new ArgumentNullException(nameof(dns));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"cluster/sdn/dns/{Uri.EscapeDataString(dns)}")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/sdn/controllers").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveSdnController[]>() ?? Array.Empty<PveSdnController>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Creates an SDN controller. Requires PVE 8.0+.
        /// </summary>
        public void CreateSdnController(PveSession session, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync("cluster/sdn/controllers", config).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Removes an SDN controller. Requires PVE 8.0+.
        /// </summary>
        public void RemoveSdnController(PveSession session, string controller)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(controller)) throw new ArgumentNullException(nameof(controller));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"cluster/sdn/controllers/{Uri.EscapeDataString(controller)}")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // SDN Update operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Applies pending SDN configuration changes cluster-wide.
        /// </summary>
        public void ApplySdn(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync("cluster/sdn", new Dictionary<string, string>())
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an SDN zone configuration.
        /// </summary>
        public void UpdateSdnZone(PveSession session, string zone, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(zone)) throw new ArgumentNullException(nameof(zone));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"cluster/sdn/zones/{Uri.EscapeDataString(zone)}", config)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an SDN VNet configuration.
        /// </summary>
        public void UpdateSdnVnet(PveSession session, string vnet, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(vnet)) throw new ArgumentNullException(nameof(vnet));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"cluster/sdn/vnets/{Uri.EscapeDataString(vnet)}", config)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an SDN subnet configuration.
        /// </summary>
        public void UpdateSdnSubnet(PveSession session, string vnet, string subnet, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(vnet)) throw new ArgumentNullException(nameof(vnet));
            if (string.IsNullOrWhiteSpace(subnet)) throw new ArgumentNullException(nameof(subnet));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync(
                    $"cluster/sdn/vnets/{Uri.EscapeDataString(vnet)}/subnets/{Uri.EscapeDataString(subnet)}",
                    config).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an SDN controller configuration.
        /// </summary>
        public void UpdateSdnController(PveSession session, string controller, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(controller)) throw new ArgumentNullException(nameof(controller));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"cluster/sdn/controllers/{Uri.EscapeDataString(controller)}", config)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an SDN IPAM plugin configuration.
        /// </summary>
        public void UpdateSdnIpam(PveSession session, string ipam, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(ipam)) throw new ArgumentNullException(nameof(ipam));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"cluster/sdn/ipams/{Uri.EscapeDataString(ipam)}", config)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an SDN DNS plugin configuration.
        /// </summary>
        public void UpdateSdnDns(PveSession session, string dns, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(dns)) throw new ArgumentNullException(nameof(dns));
            if (config == null) throw new ArgumentNullException(nameof(config));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"cluster/sdn/dns/{Uri.EscapeDataString(dns)}", config)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
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
