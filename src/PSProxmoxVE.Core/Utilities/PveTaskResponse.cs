using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Utilities
{
    /// <summary>
    /// Parses the task returned by a mutating Proxmox VE API call. Most such endpoints return
    /// the UPID as a bare string in <c>data</c>; a few return a task object directly.
    /// </summary>
    public static class PveTaskResponse
    {
        /// <summary>
        /// Parses <paramref name="json"/> as a Proxmox VE API envelope and extracts the task
        /// its <c>data</c> field describes, stamping <see cref="PveTask.Node"/> with
        /// <paramref name="node"/>.
        /// </summary>
        /// <param name="json">The raw JSON response body.</param>
        /// <param name="node">The cluster node the request was made against.</param>
        /// <returns>
        /// A <see cref="PveTask"/> with <see cref="PveTask.Upid"/> and
        /// <see cref="PveTask.Status"/> set to <c>"running"</c> when <c>data</c> is a UPID
        /// string; the deserialized task when <c>data</c> is an object; or an empty task
        /// when <c>data</c> is null or absent.
        /// </returns>
        public static PveTask Parse(string json, string node)
        {
            var data = JObject.Parse(json)["data"];
            if (data?.Type == JTokenType.String)
                return new PveTask { Upid = data.ToString(), Node = node, Status = "running" };

            var task = data?.ToObject<PveTask>() ?? new PveTask();
            task.Node = node;
            return task;
        }
    }
}
