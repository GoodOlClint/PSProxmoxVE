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
    public class UserService
    {
        // -------------------------------------------------------------------------
        // Users
        // -------------------------------------------------------------------------

        /// <summary>Returns all users.</summary>
        public PveUser[] GetUsers(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync("access/users").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveUser[]>() ?? Array.Empty<PveUser>();
        }

        /// <summary>Returns a single user by their user ID (e.g. "admin@pam").</summary>
        public PveUser GetUser(PveSession session, string userId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            using var client = new PveHttpClient(session);
            var encodedId = Uri.EscapeDataString(userId);
            var response = client.GetAsync($"access/users/{encodedId}").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            var user = data?.ToObject<PveUser>() ?? new PveUser();
            // The single-user endpoint may not echo back the userid
            if (string.IsNullOrEmpty(user.UserId))
                user.UserId = userId;
            return user;
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
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

            using var client = new PveHttpClient(session);
            client.PostAsync("access/users", formData).GetAwaiter().GetResult();
        }

        /// <summary>Removes a user account.</summary>
        public void RemoveUser(PveSession session, string userId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            using var client = new PveHttpClient(session);
            var encodedId = Uri.EscapeDataString(userId);
            client.DeleteAsync($"access/users/{encodedId}").GetAwaiter().GetResult();
        }

        /// <summary>Updates one or more properties of an existing user.</summary>
        public void SetUser(
            PveSession session,
            string userId,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            var encodedId = Uri.EscapeDataString(userId);
            var formData = config.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
            client.PutAsync($"access/users/{encodedId}", formData).GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // API Tokens
        // -------------------------------------------------------------------------

        /// <summary>Returns all API tokens for the specified user.</summary>
        public PveApiToken[] GetApiTokens(PveSession session, string userId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));

            using var client = new PveHttpClient(session);
            var encodedId = Uri.EscapeDataString(userId);
            var response = client.GetAsync($"access/users/{encodedId}/token").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            var tokens = data?.ToObject<PveApiToken[]>() ?? Array.Empty<PveApiToken>();
            foreach (var t in tokens)
                t.UserId = userId;
            return tokens;
        }

        /// <summary>
        /// Creates a new API token for the specified user and returns the token object,
        /// including the secret <c>Value</c> (shown only once).
        /// </summary>
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

            using var client = new PveHttpClient(session);
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
        }

        /// <summary>Removes an API token.</summary>
        public void RemoveApiToken(PveSession session, string userId, string tokenId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(userId))  throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentNullException(nameof(tokenId));

            using var client = new PveHttpClient(session);
            var encodedUser  = Uri.EscapeDataString(userId);
            var encodedToken = Uri.EscapeDataString(tokenId);
            client.DeleteAsync($"access/users/{encodedUser}/token/{encodedToken}")
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // Roles
        // -------------------------------------------------------------------------

        /// <summary>Returns all roles.</summary>
        public PveRole[] GetRoles(PveSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync("access/roles").GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveRole[]>() ?? Array.Empty<PveRole>();
        }

        /// <summary>Creates a new role.</summary>
        /// <param name="roleId">Role name.</param>
        /// <param name="privileges">Comma-separated list of privilege strings.</param>
        public void CreateRole(PveSession session, string roleId, string? privileges = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(roleId)) throw new ArgumentNullException(nameof(roleId));

            var formData = new Dictionary<string, string> { ["roleid"] = roleId };
            if (!string.IsNullOrEmpty(privileges))
                formData["privs"] = privileges!;

            using var client = new PveHttpClient(session);
            client.PostAsync("access/roles", formData).GetAwaiter().GetResult();
        }

        /// <summary>Removes a role.</summary>
        public void RemoveRole(PveSession session, string roleId)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(roleId)) throw new ArgumentNullException(nameof(roleId));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"access/roles/{Uri.EscapeDataString(roleId)}")
                .GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------------
        // Permissions / ACLs
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the effective permissions (resolved privilege set) for a user or token.
        /// </summary>
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

            using var client = new PveHttpClient(session);
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
                var perm = new PvePermission { Path = prop.Name };
                result.Add(perm);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Sets (adds or updates) an ACL entry.
        /// </summary>
        /// <param name="path">Access control path (e.g. "/", "/nodes/pve", "/vms/100").</param>
        /// <param name="roles">Comma-separated role IDs.</param>
        /// <param name="users">Comma-separated user IDs.</param>
        /// <param name="groups">Comma-separated group names.</param>
        /// <param name="propagate">Whether to propagate the permission to sub-paths.</param>
        /// <param name="delete">If true, removes the specified ACL entries.</param>
        public void SetPermission(
            PveSession session,
            string path,
            string roles,
            string? users = null,
            string? groups = null,
            bool propagate = true,
            bool delete = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrWhiteSpace(roles)) throw new ArgumentNullException(nameof(roles));

            var formData = new Dictionary<string, string>
            {
                ["path"] = path,
                ["roles"] = roles,
                ["propagate"] = propagate ? "1" : "0",
                ["delete"] = delete ? "1" : "0"
            };
            if (!string.IsNullOrEmpty(users)) formData["users"] = users!;
            if (!string.IsNullOrEmpty(groups)) formData["groups"] = groups!;

            using var client = new PveHttpClient(session);
            client.PutAsync("access/acl", formData).GetAwaiter().GetResult();
        }
    }
}
