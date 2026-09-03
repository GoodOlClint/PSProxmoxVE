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
    public class CloudInitService : PveServiceBase
    {
        private readonly VmService _vmService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudInitService"/> class.
        /// </summary>
        public CloudInitService()
        {
            _vmService = new VmService();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudInitService"/> class with an injected HTTP client.
        /// </summary>
        /// <param name="client">The HTTP client to use for API calls. The caller owns its lifetime.</param>
        public CloudInitService(IPveHttpClient client) : base(client)
        {
            _vmService = new VmService(client);
        }

        /// <summary>
        /// Returns the full VM configuration (delegates to <see cref="VmService.GetVmConfig"/>).
        /// Cloud-Init fields (ciuser, ipconfig*, etc.) are present as ordinary properties of
        /// the returned config alongside every other setting.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        public PveVmConfig GetFullVmConfig(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return _vmService.GetVmConfig(session, node, vmid);
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

            Invoke(session, client =>
            {
                var formData = config.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty);
                client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/config", formData)
                    .GetAwaiter().GetResult();
            });
        }

        /// <summary>
        /// Regenerates the Cloud-Init ISO drive attached to the VM
        /// (PUT /nodes/{node}/qemu/{vmid}/cloudinit).
        /// </summary>
        /// <returns>The UPID of the regeneration task.</returns>
        public string RegenerateCloudInitImage(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.PutAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/cloudinit", null)
                    .GetAwaiter().GetResult();
                var data = JObject.Parse(response)["data"];
                return data?.ToString() ?? string.Empty;
            });
        }
    }
}
