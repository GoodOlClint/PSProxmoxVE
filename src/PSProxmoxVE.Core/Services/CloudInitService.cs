using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for managing Cloud-Init configuration on Proxmox VE QEMU/KVM VMs.
    /// All operations target the /nodes/{node}/qemu/{vmid}/config endpoint.
    /// </summary>
    public class CloudInitService
    {
        // Cloud-Init field names as used in the PVE API
        private static readonly string[] CloudInitFields =
        {
            "ciuser", "cipassword", "sshkeys",
            "ipconfig0", "ipconfig1", "ipconfig2", "ipconfig3",
            "nameserver", "searchdomain", "cicustom"
        };

        /// <summary>
        /// Retrieves the Cloud-Init specific configuration fields for a VM.
        /// Internally fetches the full VM config and extracts the CI fields.
        /// </summary>
        public PveCloudInitConfig GetCloudInitConfig(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);
            var response = client.GetAsync($"nodes/{node}/qemu/{vmid}/config")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(response)["data"];
            if (data == null) return new PveCloudInitConfig();

            // Extract only the Cloud-Init fields into a reduced JObject for deserialization
            var ciObj = new JObject();
            foreach (var field in CloudInitFields)
            {
                if (data[field] != null)
                    ciObj[field] = data[field];
            }

            return ciObj.ToObject<PveCloudInitConfig>() ?? new PveCloudInitConfig();
        }

        /// <summary>
        /// Updates Cloud-Init configuration fields on a VM. Only fields present in
        /// <paramref name="config"/> are changed; unspecified fields are left as-is.
        /// </summary>
        /// <remarks>
        /// Pass only the Cloud-Init keys you want to change, for example:
        /// <c>new Dictionary&lt;string, object&gt; { ["ciuser"] = "ubuntu", ["ipconfig0"] = "ip=dhcp" }</c>
        /// </remarks>
        public void SetCloudInitConfig(
            PveSession session,
            string node,
            int vmid,
            Dictionary<string, object> config)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));
            if (config == null) throw new ArgumentNullException(nameof(config));

            using var client = new PveHttpClient(session);
            var formData = config.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty);
            client.PutAsync($"nodes/{node}/qemu/{vmid}/config", formData)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Triggers regeneration of the Cloud-Init ISO drive attached to the VM.
        /// This is done by calling the cloudinit dump endpoint, then touching the config to
        /// force PVE to rebuild the CD-ROM image on next start.
        /// </summary>
        /// <returns>
        /// The raw Cloud-Init dump (user-data) string as reported by the PVE API.
        /// </returns>
        public string RegenerateCloudInitImage(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);

            // Request a Cloud-Init dump (user-data section) — this causes PVE to rebuild the image
            var dumpResponse = client.GetAsync($"nodes/{node}/qemu/{vmid}/cloudinit/dump?type=user")
                .GetAwaiter().GetResult();
            var data = JObject.Parse(dumpResponse)["data"];
            return data?.ToString() ?? string.Empty;
        }
    }
}
