using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Firewall;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE Firewall API operations.
    /// Supports firewall management at cluster, node, VM, and container levels.
    /// </summary>
    public class FirewallService
    {
        // -------------------------------------------------------------------------
        // Rules
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns firewall rules at the specified level.
        /// </summary>
        public PveFirewallRule[] GetRules(PveSession session, string level, string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"{basePath}/rules").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveFirewallRule[]>() ?? Array.Empty<PveFirewallRule>();
        }

        /// <summary>
        /// Creates a firewall rule at the specified level.
        /// </summary>
        public void CreateRule(PveSession session, string level, Dictionary<string, string> config,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (config == null) throw new ArgumentNullException(nameof(config));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            client.PostAsync($"{basePath}/rules", config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates a firewall rule at the specified position.
        /// </summary>
        public void UpdateRule(PveSession session, string level, int pos, Dictionary<string, string> config,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (config == null) throw new ArgumentNullException(nameof(config));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            client.PutAsync($"{basePath}/rules/{pos}", config).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a firewall rule at the specified position.
        /// </summary>
        public void RemoveRule(PveSession session, string level, int pos,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"{basePath}/rules/{pos}").GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // Security Groups (cluster level only)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all firewall security groups.
        /// </summary>
        public PveFirewallGroup[] GetGroups(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync("cluster/firewall/groups").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveFirewallGroup[]>() ?? Array.Empty<PveFirewallGroup>();
        }

        /// <summary>
        /// Creates a firewall security group.
        /// </summary>
        public void CreateGroup(PveSession session, string name, string? comment = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            var formData = new Dictionary<string, string> { ["group"] = name };
            if (!string.IsNullOrEmpty(comment))
                formData["comment"] = comment!;

            using var client = new PveHttpClient(session);
            client.PostAsync("cluster/firewall/groups", formData).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a firewall security group.
        /// </summary>
        public void RemoveGroup(PveSession session, string name)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"cluster/firewall/groups/{Uri.EscapeDataString(name)}")
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns firewall rules within a security group.
        /// </summary>
        public PveFirewallRule[] GetGroupRules(PveSession session, string group)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException(nameof(group));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"cluster/firewall/groups/{Uri.EscapeDataString(group)}")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveFirewallRule[]>() ?? Array.Empty<PveFirewallRule>();
        }

        /// <summary>
        /// Creates a rule within a security group.
        /// </summary>
        public void CreateGroupRule(PveSession session, string group, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException(nameof(group));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            client.PostAsync($"cluster/firewall/groups/{Uri.EscapeDataString(group)}", config)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates a rule within a security group.
        /// </summary>
        public void UpdateGroupRule(PveSession session, string group, int pos, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException(nameof(group));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            client.PutAsync($"cluster/firewall/groups/{Uri.EscapeDataString(group)}/{pos}", config)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a rule from a security group.
        /// </summary>
        public void RemoveGroupRule(PveSession session, string group, int pos)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException(nameof(group));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"cluster/firewall/groups/{Uri.EscapeDataString(group)}/{pos}")
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // Aliases
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns firewall aliases at the specified level.
        /// </summary>
        public PveFirewallAlias[] GetAliases(PveSession session, string level, string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"{basePath}/aliases").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveFirewallAlias[]>() ?? Array.Empty<PveFirewallAlias>();
        }

        /// <summary>
        /// Creates a firewall alias.
        /// </summary>
        public void CreateAlias(PveSession session, string level, string name, string cidr,
            string? comment = null, string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(cidr)) throw new ArgumentNullException(nameof(cidr));
            var basePath = BuildBasePath(level, node, vmid);

            var formData = new Dictionary<string, string>
            {
                ["name"] = name,
                ["cidr"] = cidr
            };
            if (!string.IsNullOrEmpty(comment))
                formData["comment"] = comment!;

            using var client = new PveHttpClient(session);
            client.PostAsync($"{basePath}/aliases", formData).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates a firewall alias.
        /// </summary>
        public void UpdateAlias(PveSession session, string level, string name,
            string? cidr = null, string? comment = null, string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var basePath = BuildBasePath(level, node, vmid);

            var formData = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(cidr))
                formData["cidr"] = cidr!;
            if (!string.IsNullOrEmpty(comment))
                formData["comment"] = comment!;

            using var client = new PveHttpClient(session);
            client.PutAsync($"{basePath}/aliases/{Uri.EscapeDataString(name)}", formData)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a firewall alias.
        /// </summary>
        public void RemoveAlias(PveSession session, string level, string name,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"{basePath}/aliases/{Uri.EscapeDataString(name)}")
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // IP Sets
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns firewall IP sets at the specified level.
        /// </summary>
        public PveFirewallIpSet[] GetIpSets(PveSession session, string level, string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"{basePath}/ipset").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveFirewallIpSet[]>() ?? Array.Empty<PveFirewallIpSet>();
        }

        /// <summary>
        /// Creates a firewall IP set.
        /// </summary>
        public void CreateIpSet(PveSession session, string level, string name,
            string? comment = null, string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var basePath = BuildBasePath(level, node, vmid);

            var formData = new Dictionary<string, string> { ["name"] = name };
            if (!string.IsNullOrEmpty(comment))
                formData["comment"] = comment!;

            using var client = new PveHttpClient(session);
            client.PostAsync($"{basePath}/ipset", formData).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a firewall IP set.
        /// </summary>
        public void RemoveIpSet(PveSession session, string level, string name,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"{basePath}/ipset/{Uri.EscapeDataString(name)}")
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // IP Set Entries
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns entries within an IP set.
        /// </summary>
        public PveFirewallIpSetEntry[] GetIpSetEntries(PveSession session, string level, string name,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"{basePath}/ipset/{Uri.EscapeDataString(name)}")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveFirewallIpSetEntry[]>() ?? Array.Empty<PveFirewallIpSetEntry>();
        }

        /// <summary>
        /// Adds an entry to an IP set.
        /// </summary>
        public void AddIpSetEntry(PveSession session, string level, string name, string cidr,
            bool nomatch = false, string? comment = null, string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(cidr)) throw new ArgumentNullException(nameof(cidr));
            var basePath = BuildBasePath(level, node, vmid);

            var formData = new Dictionary<string, string> { ["cidr"] = cidr };
            if (nomatch)
                formData["nomatch"] = "1";
            if (!string.IsNullOrEmpty(comment))
                formData["comment"] = comment!;

            using var client = new PveHttpClient(session);
            client.PostAsync($"{basePath}/ipset/{Uri.EscapeDataString(name)}", formData)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates an entry in an IP set.
        /// </summary>
        public void UpdateIpSetEntry(PveSession session, string level, string name, string cidr,
            bool? nomatch = null, string? comment = null, string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(cidr)) throw new ArgumentNullException(nameof(cidr));
            var basePath = BuildBasePath(level, node, vmid);

            var formData = new Dictionary<string, string>();
            if (nomatch.HasValue)
                formData["nomatch"] = nomatch.Value ? "1" : "0";
            if (!string.IsNullOrEmpty(comment))
                formData["comment"] = comment!;

            using var client = new PveHttpClient(session);
            client.PutAsync(
                $"{basePath}/ipset/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(cidr)}",
                formData).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes an entry from an IP set.
        /// </summary>
        public void RemoveIpSetEntry(PveSession session, string level, string name, string cidr,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(cidr)) throw new ArgumentNullException(nameof(cidr));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            client.DeleteAsync(
                $"{basePath}/ipset/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(cidr)}")
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // Options
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns firewall options at the specified level.
        /// </summary>
        public PveFirewallOptions GetOptions(PveSession session, string level,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"{basePath}/options").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveFirewallOptions>() ?? new PveFirewallOptions();
        }

        /// <summary>
        /// Sets firewall options at the specified level.
        /// </summary>
        public void SetOptions(PveSession session, string level, Dictionary<string, string> config,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (config == null) throw new ArgumentNullException(nameof(config));
            var basePath = BuildBasePath(level, node, vmid);

            using var client = new PveHttpClient(session);
            client.PutAsync($"{basePath}/options", config).GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // Refs
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns firewall references (aliases, IP sets) at the specified level.
        /// </summary>
        public PveFirewallRef[] GetRefs(PveSession session, string level, string? type = null,
            string? node = null, int? vmid = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var basePath = BuildBasePath(level, node, vmid);

            var resource = $"{basePath}/refs";
            if (!string.IsNullOrEmpty(type))
                resource += $"?type={Uri.EscapeDataString(type!)}";

            using var client = new PveHttpClient(session);
            var response = client.GetAsync(resource).GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveFirewallRef[]>() ?? Array.Empty<PveFirewallRef>();
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Builds the API base path for the specified firewall level.
        /// </summary>
        private static string BuildBasePath(string level, string? node, int? vmid)
        {
            switch (level.ToLowerInvariant())
            {
                case "cluster":
                    return "cluster/firewall";
                case "node":
                    if (string.IsNullOrWhiteSpace(node))
                        throw new ArgumentException("Node is required for node-level firewall operations.", nameof(node));
                    return $"nodes/{Uri.EscapeDataString(node)}/firewall";
                case "vm":
                    if (string.IsNullOrWhiteSpace(node))
                        throw new ArgumentException("Node is required for VM-level firewall operations.", nameof(node));
                    if (!vmid.HasValue)
                        throw new ArgumentException("VmId is required for VM-level firewall operations.", nameof(vmid));
                    return $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid.Value}/firewall";
                case "container":
                    if (string.IsNullOrWhiteSpace(node))
                        throw new ArgumentException("Node is required for container-level firewall operations.", nameof(node));
                    if (!vmid.HasValue)
                        throw new ArgumentException("VmId is required for container-level firewall operations.", nameof(vmid));
                    return $"nodes/{Uri.EscapeDataString(node)}/lxc/{vmid.Value}/firewall";
                default:
                    throw new ArgumentException($"Invalid firewall level: {level}. Must be Cluster, Node, Vm, or Container.", nameof(level));
            }
        }
    }
}
