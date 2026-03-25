using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.HA;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE High Availability (HA) API operations.
    /// </summary>
    public class HaService
    {
        private readonly IPveHttpClient? _injectedClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HaService"/> class.
        /// </summary>
        public HaService() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HaService"/> class with an injected HTTP client.
        /// </summary>
        /// <param name="client">The HTTP client to use for API calls. The caller owns its lifetime.</param>
        public HaService(IPveHttpClient client)
        {
            _injectedClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        // -------------------------------------------------------------------------
        // Resources
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all HA resources.
        /// </summary>
        public PveHaResource[] GetResources(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/ha/resources").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveHaResource[]>() ?? Array.Empty<PveHaResource>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns a single HA resource by its SID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="sid">The resource ID, e.g. "vm:100" or "ct:200".</param>
        public PveHaResource GetResource(PveSession session, string sid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(sid)) throw new ArgumentNullException(nameof(sid));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"cluster/ha/resources/{Uri.EscapeDataString(sid)}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveHaResource>() ?? new PveHaResource();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Creates a new HA resource.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="sid">The resource ID, e.g. "vm:100" or "ct:200".</param>
        /// <param name="options">Additional configuration options (state, group, max_relocate, etc.).</param>
        public void CreateResource(PveSession session, string sid, Dictionary<string, string> options)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(sid)) throw new ArgumentNullException(nameof(sid));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var formData = new Dictionary<string, string>(options) { ["sid"] = sid };

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync("cluster/ha/resources", formData).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an existing HA resource.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="sid">The resource ID, e.g. "vm:100" or "ct:200".</param>
        /// <param name="options">Configuration options to update.</param>
        public void UpdateResource(PveSession session, string sid, Dictionary<string, string> options)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(sid)) throw new ArgumentNullException(nameof(sid));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"cluster/ha/resources/{Uri.EscapeDataString(sid)}", options)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Deletes an HA resource.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="sid">The resource ID, e.g. "vm:100" or "ct:200".</param>
        public void DeleteResource(PveSession session, string sid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(sid)) throw new ArgumentNullException(nameof(sid));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"cluster/ha/resources/{Uri.EscapeDataString(sid)}")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Requests migration of an HA resource to another node. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="sid">The resource ID, e.g. "vm:100" or "ct:200".</param>
        /// <param name="node">The target node to migrate to.</param>
        public string MigrateResource(PveSession session, string sid, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(sid)) throw new ArgumentNullException(nameof(sid));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var formData = new Dictionary<string, string> { ["node"] = node };

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync(
                    $"cluster/ha/resources/{Uri.EscapeDataString(sid)}/migrate", formData)
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToString() ?? string.Empty;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Requests relocation of an HA resource to another node. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="sid">The resource ID, e.g. "vm:100" or "ct:200".</param>
        /// <param name="node">The target node to relocate to.</param>
        public string RelocateResource(PveSession session, string sid, string node)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(sid)) throw new ArgumentNullException(nameof(sid));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            var formData = new Dictionary<string, string> { ["node"] = node };

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.PostAsync(
                    $"cluster/ha/resources/{Uri.EscapeDataString(sid)}/relocate", formData)
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToString() ?? string.Empty;
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Groups
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all HA groups.
        /// </summary>
        public PveHaGroup[] GetGroups(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/ha/groups").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveHaGroup[]>() ?? Array.Empty<PveHaGroup>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns a single HA group by name.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="group">The HA group name.</param>
        public PveHaGroup GetGroup(PveSession session, string group)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException(nameof(group));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"cluster/ha/groups/{Uri.EscapeDataString(group)}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveHaGroup>() ?? new PveHaGroup();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Creates a new HA group.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="group">The group name.</param>
        /// <param name="nodes">Comma-separated list of nodes with optional priorities, e.g. "node1:2,node2:1".</param>
        /// <param name="options">Additional configuration options (restricted, nofailback, comment).</param>
        public void CreateGroup(PveSession session, string group, string nodes, Dictionary<string, string> options)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException(nameof(group));
            if (string.IsNullOrWhiteSpace(nodes)) throw new ArgumentNullException(nameof(nodes));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var formData = new Dictionary<string, string>(options)
            {
                ["group"] = group,
                ["nodes"] = nodes
            };

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync("cluster/ha/groups", formData).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an existing HA group.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="group">The group name.</param>
        /// <param name="options">Configuration options to update.</param>
        public void UpdateGroup(PveSession session, string group, Dictionary<string, string> options)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException(nameof(group));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"cluster/ha/groups/{Uri.EscapeDataString(group)}", options)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Deletes an HA group.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="group">The group name.</param>
        public void DeleteGroup(PveSession session, string group)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(group)) throw new ArgumentNullException(nameof(group));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"cluster/ha/groups/{Uri.EscapeDataString(group)}")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Status
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the current HA status from /cluster/ha/status/current.
        /// </summary>
        public PveHaStatus[] GetStatus(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/ha/status/current").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveHaStatus[]>() ?? Array.Empty<PveHaStatus>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns the full HA manager status as a raw JSON object.
        /// </summary>
        public Dictionary<string, object?> GetManagerStatus(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/ha/status/manager_status").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return JsonHelper.ToDictionary(data as JObject);
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        // -------------------------------------------------------------------------
        // Rules (PVE 9.0+)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all HA rules. Requires PVE 9.0 or later.
        /// </summary>
        public PveHaRule[] GetRules(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync("cluster/ha/rules").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveHaRule[]>() ?? Array.Empty<PveHaRule>();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Returns a single HA rule by its ID. Requires PVE 9.0 or later.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="rule">The rule identifier.</param>
        public PveHaRule GetRule(PveSession session, string rule)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(rule)) throw new ArgumentNullException(nameof(rule));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                var response = client.GetAsync($"cluster/ha/rules/{Uri.EscapeDataString(rule)}")
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveHaRule>() ?? new PveHaRule();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Creates a new HA rule. Requires PVE 9.0 or later.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="options">Rule configuration options (rule, type, state, comment, etc.).</param>
        public void CreateRule(PveSession session, Dictionary<string, string> options)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PostAsync("cluster/ha/rules", options).GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Updates an existing HA rule. Requires PVE 9.0 or later.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="rule">The rule identifier.</param>
        /// <param name="options">Configuration options to update.</param>
        public void UpdateRule(PveSession session, string rule, Dictionary<string, string> options)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(rule)) throw new ArgumentNullException(nameof(rule));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.PutAsync($"cluster/ha/rules/{Uri.EscapeDataString(rule)}", options)
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }

        /// <summary>
        /// Deletes an HA rule. Requires PVE 9.0 or later.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="rule">The rule identifier.</param>
        public void DeleteRule(PveSession session, string rule)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(rule)) throw new ArgumentNullException(nameof(rule));

            IPveHttpClient client = _injectedClient ?? new PveHttpClient(session);
            try
            {
                client.DeleteAsync($"cluster/ha/rules/{Uri.EscapeDataString(rule)}")
                    .GetAwaiter().GetResult();
            }
            finally
            {
                if (_injectedClient == null) client.Dispose();
            }
        }
    }
}
