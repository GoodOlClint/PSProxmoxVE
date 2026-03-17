using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Storage;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE storage API operations.
    /// </summary>
    public class StorageService
    {
        // -------------------------------------------------------------------------
        // Read operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns storage definitions. If <paramref name="node"/> is null, returns
        /// the cluster-wide storage list; otherwise returns storage visible on that node.
        /// </summary>
        public PveStorage[] GetStorages(PveSession session, string? node = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var resource = node != null
                ? $"nodes/{node}/storage"
                : "storage";

            using var client = new PveHttpClient(session);
            var response = client.GetAsync(resource).GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveStorage[]>() ?? Array.Empty<PveStorage>();
        }

        /// <summary>
        /// Returns the contents of a storage volume on a specific node.
        /// </summary>
        /// <param name="contentType">
        /// Optional content type filter (e.g. "iso", "vztmpl", "images", "backup").
        /// </param>
        public PveStorageContent[] GetStorageContent(
            PveSession session,
            string node,
            string storage,
            string? contentType = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(storage)) throw new ArgumentNullException(nameof(storage));

            var resource = $"nodes/{node}/storage/{storage}/content";
            if (!string.IsNullOrEmpty(contentType))
                resource += $"?content={Uri.EscapeDataString(contentType!)}";

            using var client = new PveHttpClient(session);
            var response = client.GetAsync(resource).GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveStorageContent[]>() ?? Array.Empty<PveStorageContent>();
        }

        // -------------------------------------------------------------------------
        // Upload / download
        // -------------------------------------------------------------------------

        /// <summary>
        /// Uploads an ISO (or other file) to a storage on a node. Returns the task UPID.
        /// </summary>
        /// <param name="checksum">Optional checksum value.</param>
        /// <param name="checksumAlgorithm">Optional checksum algorithm (e.g. "sha256").</param>
        /// <param name="progressCallback">Optional callback with (bytesSent, totalBytes).</param>
        public PveTask UploadIso(
            PveSession session,
            string node,
            string storage,
            string filePath,
            string? checksum = null,
            string? checksumAlgorithm = null,
            Action<long, long>? progressCallback = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(storage)) throw new ArgumentNullException(nameof(storage));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            var formFields = new Dictionary<string, string>
            {
                ["content"] = "iso"
            };

            using var client = new PveHttpClient(session);
            var response = client.UploadFileAsync(
                    $"nodes/{node}/storage/{storage}/upload",
                    filePath,
                    formFields,
                    checksum,
                    checksumAlgorithm,
                    progressCallback)
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>
        /// Downloads a file from a URL directly to a storage on a node. Returns the task UPID.
        /// </summary>
        public PveTask DownloadUrl(
            PveSession session,
            string node,
            string storage,
            string url,
            string filename,
            string contentType)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrWhiteSpace(storage)) throw new ArgumentNullException(nameof(storage));
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentNullException(nameof(filename));
            if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentNullException(nameof(contentType));

            var formData = new Dictionary<string, string>
            {
                ["url"] = url,
                ["filename"] = filename,
                ["content"] = contentType
            };

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/storage/{storage}/download-url", formData)
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        // -------------------------------------------------------------------------
        // Storage CRUD
        // -------------------------------------------------------------------------

        /// <summary>
        /// Creates a new storage definition at the cluster level. Returns the task UPID or
        /// null if the API returns no task (some storage types apply immediately).
        /// </summary>
        public PveStorage CreateStorage(PveSession session, Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            var formData = config.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
            var response = client.PostAsync("storage", formData).GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            return data?.ToObject<PveStorage>() ?? new PveStorage();
        }

        /// <summary>
        /// Removes a cluster-level storage definition.
        /// </summary>
        public void RemoveStorage(PveSession session, string storage)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(storage)) throw new ArgumentNullException(nameof(storage));

            using var client = new PveHttpClient(session);
            client.DeleteAsync($"storage/{storage}").GetAwaiter().GetResult();
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
