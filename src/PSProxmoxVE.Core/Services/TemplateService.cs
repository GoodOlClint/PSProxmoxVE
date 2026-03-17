using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Services
{
    /// <summary>
    /// Service for Proxmox VE VM template operations.
    /// Templates are VMs with the "template" flag set to 1.
    /// </summary>
    public class TemplateService
    {
        private readonly VmService _vmService = new VmService();

        /// <summary>
        /// Returns all VM templates. If <paramref name="node"/> is null, searches all cluster nodes.
        /// </summary>
        public PveVm[] GetTemplates(PveSession session, string? node = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var vms = _vmService.GetVms(session, node);
            return vms.Where(v => v.Template == 1).ToArray();
        }

        /// <summary>
        /// Converts an existing VM into a template. Returns the task UPID.
        /// </summary>
        /// <remarks>
        /// The VM must be stopped and must not already be a template.
        /// Once converted, this operation cannot be reversed via the API.
        /// </remarks>
        public PveTask CreateTemplate(PveSession session, string node, int vmid)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(node)) throw new ArgumentNullException(nameof(node));

            using var client = new PveHttpClient(session);
            var response = client.PostAsync($"nodes/{node}/qemu/{vmid}/template")
                .GetAwaiter().GetResult();
            return ParseTask(response, node);
        }

        /// <summary>
        /// Removes a VM template (delegates to <see cref="VmService.RemoveVm"/>).
        /// </summary>
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
