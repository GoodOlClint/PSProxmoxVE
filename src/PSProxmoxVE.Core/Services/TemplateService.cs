using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE VM template operations.
    /// Templates are VMs with the "template" flag set to 1.
    /// </summary>
    public class TemplateService : PveServiceBase
    {
        private readonly VmService _vmService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateService"/> class.
        /// </summary>
        public TemplateService()
        {
            _vmService = new VmService();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateService"/> class with an injected HTTP client.
        /// </summary>
        /// <param name="client">The HTTP client to use for API calls. The caller owns its lifetime.</param>
        public TemplateService(IPveHttpClient client) : base(client)
        {
            _vmService = new VmService(client);
        }

        /// <summary>
        /// Returns all VM templates. If <paramref name="node"/> is null, searches all cluster nodes.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">Optional cluster node name to filter templates by node.</param>
        /// <param name="onNodeSkipped">
        /// Forwarded to <see cref="VmService.GetVms"/>, which does not invoke it for the
        /// all-nodes listing (a single <c>cluster/resources</c> call has no per-node
        /// failure to report); kept for source compatibility with existing callers.
        /// </param>
        public PveVm[] GetTemplates(PveSession session, string? node = null, Action<string, Exception>? onNodeSkipped = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var vms = _vmService.GetVms(session, node, onNodeSkipped);
            return vms.Where(v => v.Template == 1).ToArray();
        }

        /// <summary>
        /// Converts an existing VM into a template. Returns the task UPID.
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <remarks>
        /// The VM must be stopped and must not already be a template.
        /// Once converted, this operation cannot be reversed via the API.
        /// </remarks>
        public PveTask CreateTemplate(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return Invoke(session, client =>
            {
                var response = client.PostAsync($"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/template")
                    .GetAwaiter().GetResult();
                return PveTaskResponse.Parse(response, node);
            });
        }

        /// <summary>
        /// Removes a VM template (delegates to <see cref="VmService.RemoveVm"/>).
        /// </summary>
        /// <param name="session">The authenticated PVE session.</param>
        /// <param name="node">The cluster node name.</param>
        /// <param name="vmid">The VM ID.</param>
        /// <param name="purge">
        /// If true, also removes all associated backup files and jobs.
        /// </param>
        public PveTask RemoveTemplate(
            PveSession session,
            string node,
            int vmid,
            bool purge = false)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            return _vmService.RemoveVm(session, node, vmid, purge);
        }
    }
}
