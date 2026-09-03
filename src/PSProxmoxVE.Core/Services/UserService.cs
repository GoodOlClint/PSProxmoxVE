using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Users;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE access control — users, roles, and permissions (ACLs).
    /// </summary>
    public class UserService : PveServiceBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UserService"/> with no injected client.
        /// Each method will create and dispose its own <see cref="PveHttpClient"/>.
        /// </summary>
        public UserService() { }

        /// <summary>
        /// Initializes a new instance of <see cref="UserService"/> with an injected HTTP client.
        /// The caller owns the client's lifetime; this service will not dispose it.
        /// </summary>
        /// <param name="client">The HTTP client to use for all requests.</param>
        public UserService(IPveHttpClient client) : base(client) { }

        // -------------------------------------------------------------------------
        // Users
        // -------------------------------------------------------------------------

        /// <summary>Returns all users.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        public PveUser[] GetUsers(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("access/users").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveUser[]>() ?? Array.Empty<PveUser>();
            });
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="userId">User ID in "username@realm" format.</param>
        /// <param name="config">Additional fields (password, email, firstname, lastname, etc.).</param>
        public void CreateUser(
            PveSession session,
            string userId,
            Dictionary<string, object>? config = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            var formData = new Dictionary<string, string> { ["userid"] = userId };
            if (config != null)
            {
                foreach (var kvp in config)
                    formData[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            }

            Invoke(session, client =>
            {
                client.PostAsync("access/users", formData).GetAwaiter().GetResult();
            });
        }

        /// <summary>Removes a user account.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="userId">The user ID in "username@realm" format.</param>
        public void RemoveUser(PveSession session, string userId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            Invoke(session, client =>
            {
                var encodedId = Uri.EscapeDataString(userId);
                client.DeleteAsync($"access/users/{encodedId}").GetAwaiter().GetResult();
            });
        }

        /// <summary>Updates one or more properties of an existing user.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="userId">The user ID in "username@realm" format.</param>
        /// <param name="config">User properties to update.</param>
        public void SetUser(
            PveSession session,
            string userId,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Invoke(session, client =>
            {
                var encodedId = Uri.EscapeDataString(userId);
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                client.PutAsync($"access/users/{encodedId}", formData).GetAwaiter().GetResult();
            });
        }

        // -------------------------------------------------------------------------
        // API Tokens
        // -------------------------------------------------------------------------

        /// <summary>Returns all API tokens for the specified user.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="userId">The user ID in "username@realm" format.</param>
        public PveApiToken[] GetApiTokens(PveSession session, string userId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            return Invoke(session, client =>
            {
                var encodedId = Uri.EscapeDataString(userId);
                var response = client.GetAsync($"access/users/{encodedId}/token").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                var tokens = data?.ToObject<PveApiToken[]>() ?? Array.Empty<PveApiToken>();
                foreach (var t in tokens)
                    t.UserId = userId;
                return tokens;
            });
        }

        /// <summary>
        /// Creates a new API token for the specified user and returns the token object,
        /// including the secret <c>Value</c> (shown only once).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="userId">User ID in "username@realm" format.</param>
        /// <param name="tokenId">Token identifier (alphanumeric and hyphens).</param>
        /// <param name="comment">Optional description.</param>
        /// <param name="expire">Expiry as a Unix timestamp; 0 = never.</param>
        /// <param name="privilegeSeparation">
        /// When true, the token's effective permissions are intersected with the user's ACLs.
        /// </param>
        public PveApiToken CreateApiToken(
            PveSession session,
            string userId,
            string tokenId,
            string? comment = null,
            long? expire = null,
            bool? privilegeSeparation = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId))  throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentNullException(nameof(tokenId));

            var formData = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(comment))    formData["comment"]  = comment!;
            if (expire.HasValue)                   formData["expire"]   = expire.Value.ToString();
            if (privilegeSeparation.HasValue)      formData["privsep"]  = privilegeSeparation.Value ? "1" : "0";

            return Invoke(session, client =>
            {
                var encodedUser  = Uri.EscapeDataString(userId);
                var encodedToken = Uri.EscapeDataString(tokenId);
                var response = client.PostAsync(
                    $"access/users/{encodedUser}/token/{encodedToken}", formData)
                    .GetAwaiter().GetResult();

                var data = JObject.Parse(response)["data"];
                var token = data?.ToObject<PveApiToken>() ?? new PveApiToken();
                token.UserId  = userId;
                token.TokenId = tokenId;
                return token;
            });
        }

        /// <summary>Removes an API token.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="userId">The user ID in "username@realm" format.</param>
        /// <param name="tokenId">The token identifier to remove.</param>
        public void RemoveApiToken(PveSession session, string userId, string tokenId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId))  throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentNullException(nameof(tokenId));

            Invoke(session, client =>
            {
                var encodedUser  = Uri.EscapeDataString(userId);
                var encodedToken = Uri.EscapeDataString(tokenId);
                client.DeleteAsync($"access/users/{encodedUser}/token/{encodedToken}")
                    .GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Updates an API token's configuration.
        /// </summary>
        public void UpdateApiToken(PveSession session, string userId, string tokenId, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentNullException(nameof(tokenId));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Invoke(session, client =>
            {
                var encodedUser = Uri.EscapeDataString(userId);
                var encodedToken = Uri.EscapeDataString(tokenId);
                client.PutAsync($"access/users/{encodedUser}/token/{encodedToken}", config)
                    .GetAwaiter().GetResult();
            });
        }

        // -------------------------------------------------------------------------
        // Roles
        // -------------------------------------------------------------------------

        /// <summary>Returns all roles.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        public PveRole[] GetRoles(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("access/roles").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveRole[]>() ?? Array.Empty<PveRole>();
            });
        }

        /// <summary>Creates a new role.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="roleId">Role name.</param>
        /// <param name="privileges">Comma-separated list of privilege strings.</param>
        public void CreateRole(PveSession session, string roleId, string? privileges = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(roleId)) throw new ArgumentNullException(nameof(roleId));

            var formData = new Dictionary<string, string> { ["roleid"] = roleId };
            if (!string.IsNullOrEmpty(privileges))
                formData["privs"] = privileges!;

            Invoke(session, client =>
            {
                client.PostAsync("access/roles", formData).GetAwaiter().GetResult();
            });
        }

        /// <summary>Removes a role.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="roleId">The role name to remove.</param>
        public void RemoveRole(PveSession session, string roleId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(roleId)) throw new ArgumentNullException(nameof(roleId));

            Invoke(session, client =>
            {
                client.DeleteAsync($"access/roles/{Uri.EscapeDataString(roleId)}")
                    .GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Updates a role's privileges.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="roleId">The role identifier to update.</param>
        /// <param name="privileges">Comma-separated list of privileges.</param>
        public void UpdateRole(PveSession session, string roleId, string privileges)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(roleId)) throw new ArgumentNullException(nameof(roleId));
            if (string.IsNullOrWhiteSpace(privileges)) throw new ArgumentNullException(nameof(privileges));

            var formData = new Dictionary<string, string> { ["privs"] = privileges };
            Invoke(session, client =>
            {
                client.PutAsync($"access/roles/{Uri.EscapeDataString(roleId)}", formData)
                    .GetAwaiter().GetResult();
            });
        }

        // -------------------------------------------------------------------------
        // Groups
        // -------------------------------------------------------------------------

        /// <summary>Returns all groups.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        public PveGroup[] GetGroups(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("access/groups").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveGroup[]>() ?? Array.Empty<PveGroup>();
            });
        }

        /// <summary>Creates a new group.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="comment">Optional comment/description.</param>
        public void CreateGroup(PveSession session, string groupId, string? comment = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(groupId)) throw new ArgumentNullException(nameof(groupId));

            var formData = new Dictionary<string, string> { ["groupid"] = groupId };
            if (!string.IsNullOrEmpty(comment))
                formData["comment"] = comment!;

            Invoke(session, client =>
            {
                client.PostAsync("access/groups", formData).GetAwaiter().GetResult();
            });
        }

        /// <summary>Updates a group's properties.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="groupId">The group identifier to update.</param>
        /// <param name="config">Configuration parameters to update.</param>
        public void UpdateGroup(PveSession session, string groupId, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(groupId)) throw new ArgumentNullException(nameof(groupId));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Invoke(session, client =>
            {
                client.PutAsync($"access/groups/{Uri.EscapeDataString(groupId)}", config)
                    .GetAwaiter().GetResult();
            });
        }

        /// <summary>Removes a group.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="groupId">The group identifier to remove.</param>
        public void RemoveGroup(PveSession session, string groupId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(groupId)) throw new ArgumentNullException(nameof(groupId));

            Invoke(session, client =>
            {
                client.DeleteAsync($"access/groups/{Uri.EscapeDataString(groupId)}")
                    .GetAwaiter().GetResult();
            });
        }

        // -------------------------------------------------------------------------
        // Domains / Realms
        // -------------------------------------------------------------------------

        /// <summary>Returns all authentication domains/realms.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        public PveDomain[] GetDomains(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            return Invoke(session, client =>
            {
                var response = client.GetAsync("access/domains").GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToObject<PveDomain[]>() ?? Array.Empty<PveDomain>();
            });
        }

        /// <summary>Creates a new authentication domain/realm.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="config">Domain configuration parameters (must include realm and type).</param>
        public void CreateDomain(PveSession session, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Invoke(session, client =>
            {
                client.PostAsync("access/domains", config).GetAwaiter().GetResult();
            });
        }

        /// <summary>Updates an authentication domain/realm.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="realm">The realm identifier to update.</param>
        /// <param name="config">Configuration parameters to update.</param>
        public void UpdateDomain(PveSession session, string realm, Dictionary<string, string> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(realm)) throw new ArgumentNullException(nameof(realm));
            if (config == null) throw new ArgumentNullException(nameof(config));

            Invoke(session, client =>
            {
                client.PutAsync($"access/domains/{Uri.EscapeDataString(realm)}", config)
                    .GetAwaiter().GetResult();
            });
        }

        /// <summary>Removes an authentication domain/realm.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="realm">The realm identifier to remove.</param>
        public void RemoveDomain(PveSession session, string realm)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(realm)) throw new ArgumentNullException(nameof(realm));

            Invoke(session, client =>
            {
                client.DeleteAsync($"access/domains/{Uri.EscapeDataString(realm)}")
                    .GetAwaiter().GetResult();
            });
        }

        // -------------------------------------------------------------------------
        // Password
        // -------------------------------------------------------------------------

        /// <summary>Changes a user's password.</summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="userId">The user ID in "username@realm" format.</param>
        /// <param name="password">The new password.</param>
        public void ChangePassword(PveSession session, string userId, string password)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));

            var formData = new Dictionary<string, string>
            {
                ["userid"] = userId,
                ["password"] = password
            };

            Invoke(session, client =>
            {
                client.PutAsync("access/password", formData).GetAwaiter().GetResult();
            });
        }

        // -------------------------------------------------------------------------
        // Permissions / ACLs
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the effective permissions (resolved privilege set) for a user or token.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="userId">Optional user ID to query; defaults to the authenticated user.</param>
        /// <param name="path">Optional path to restrict the query.</param>
        public PvePermission[] GetPermissions(
            PveSession session,
            string? userId = null,
            string? path = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var resource = "access/permissions";
            var queryParts = new List<string>();
            if (!string.IsNullOrEmpty(userId))
                queryParts.Add($"userid={Uri.EscapeDataString(userId!)}");
            if (!string.IsNullOrEmpty(path))
                queryParts.Add($"path={Uri.EscapeDataString(path!)}");
            if (queryParts.Count > 0)
                resource += "?" + string.Join("&", queryParts);

            return Invoke(session, client =>
            {
                var response = client.GetAsync(resource).GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                // /access/permissions returns an object keyed by path, not an array
                if (data == null) return Array.Empty<PvePermission>();
                if (data.Type == JTokenType.Array)
                    return data.ToObject<PvePermission[]>() ?? Array.Empty<PvePermission>();

                // Unwrap path-keyed object into flat list
                var result = new List<PvePermission>();
                foreach (var prop in ((JObject)data).Properties())
                {
                    var privileges = prop.Value is JObject privMap
                        ? privMap.Properties().ToDictionary(
                            p => p.Name,
                            p => p.Value.Type == JTokenType.Boolean
                                ? p.Value.Value<bool>()
                                : p.Value.Type == JTokenType.Integer && p.Value.Value<long>() != 0,
                            StringComparer.OrdinalIgnoreCase)
                        : null;
                    var perm = new PvePermission { Path = prop.Name, Privileges = privileges };
                    result.Add(perm);
                }
                return result.ToArray();
            });
        }

        /// <summary>
        /// Sets (adds or updates) an ACL entry.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="path">Access control path (e.g. "/", "/nodes/pve", "/vms/100").</param>
        /// <param name="roles">Comma-separated role IDs.</param>
        /// <param name="users">Comma-separated user IDs.</param>
        /// <param name="groups">Comma-separated group names.</param>
        /// <param name="tokens">Comma-separated API token IDs (user@realm!tokenid).</param>
        /// <param name="propagate">Whether to propagate the permission to sub-paths; omitted when unset.</param>
        /// <param name="delete">If true, removes the specified ACL entries; omitted when unset.</param>
        public void SetPermission(
            PveSession session,
            string path,
            string roles,
            string? users = null,
            string? groups = null,
            string? tokens = null,
            bool? propagate = null,
            bool? delete = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrWhiteSpace(roles)) throw new ArgumentNullException(nameof(roles));

            var formData = new Dictionary<string, string>
            {
                ["path"] = path,
                ["roles"] = roles
            };
            if (!string.IsNullOrEmpty(users)) formData["users"] = users!;
            if (!string.IsNullOrEmpty(groups)) formData["groups"] = groups!;
            if (!string.IsNullOrEmpty(tokens)) formData["tokens"] = tokens!;
            if (propagate.HasValue) formData["propagate"] = propagate.Value ? "1" : "0";
            if (delete.HasValue) formData["delete"] = delete.Value ? "1" : "0";

            Invoke(session, client =>
            {
                client.PutAsync("access/acl", formData).GetAwaiter().GetResult();
            });
        }
    }
}
