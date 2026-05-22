using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PSProxmoxVE.Core.Client
{
    /// <summary>
    /// Abstraction over the PVE HTTP client for testability and dependency injection.
    /// Services accept this interface via constructor injection; tests can mock it.
    /// </summary>
    public interface IPveHttpClient : IDisposable
    {
        /// <summary>Performs a GET request against the specified API resource path.</summary>
        Task<string> GetAsync(string resource);

        /// <summary>Performs a POST request against the specified API resource path.</summary>
        Task<string> PostAsync(string resource, Dictionary<string, string>? data = null);

        /// <summary>
        /// Performs a POST request whose form body may contain repeated keys, used for
        /// PVE array parameters (e.g. guest-exec "command"). Each pair becomes one
        /// <c>key=value</c> field, so a key may appear multiple times.
        /// </summary>
        Task<string> PostAsync(string resource, IEnumerable<KeyValuePair<string, string>> data);

        /// <summary>Performs a PUT request against the specified API resource path.</summary>
        Task<string> PutAsync(string resource, Dictionary<string, string>? data = null);

        /// <summary>Performs a DELETE request against the specified API resource path.</summary>
        Task<string> DeleteAsync(string resource);

        /// <summary>Synchronous wrapper for <see cref="GetAsync"/>.</summary>
        string Get(string resource);

        /// <summary>Synchronous wrapper for <see cref="PostAsync(string, Dictionary{string, string})"/>.</summary>
        string Post(string resource, Dictionary<string, string>? data = null);

        /// <summary>Synchronous wrapper for <see cref="PutAsync"/>.</summary>
        string Put(string resource, Dictionary<string, string>? data = null);

        /// <summary>Synchronous wrapper for <see cref="DeleteAsync"/>.</summary>
        string Delete(string resource);

        /// <summary>
        /// Uploads a file to a Proxmox VE storage endpoint using MultipartFormDataContent.
        /// </summary>
        Task<string> UploadFileAsync(
            string resource,
            string filePath,
            Dictionary<string, string>? formFields = null,
            string? checksum = null,
            string? checksumAlgorithm = null,
            Action<long, long>? progressCallback = null);
    }
}
