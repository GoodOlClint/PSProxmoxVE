using System;
using System.Collections.Generic;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Downloads a file from a URL directly into a Proxmox VE storage.</para>
    /// <para type="description">
    /// Instructs the Proxmox VE node to download a file (e.g., an ISO or container template)
    /// from an external URL and save it to the specified storage. Returns a PveTask.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "PveStorageDownload", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class InvokePveStorageDownloadCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node that will perform the download.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The target storage identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The storage pool name.")]
        public string Storage { get; set; } = string.Empty;

        /// <summary>The URL to download the file from.</summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "The URL to download the file from.")]
        public string Url { get; set; } = string.Empty;

        /// <summary>The filename to save the downloaded file as on the storage.</summary>
        [Parameter(Mandatory = true, Position = 3, HelpMessage = "Filename to save the download as.")]
        public string Filename { get; set; } = string.Empty;

        /// <summary>The content type category for the downloaded file. Defaults to "iso".</summary>
        [Parameter(Mandatory = false, HelpMessage = "Content type category. Defaults to iso.")]
        [ValidateSet("iso", "vztmpl", "backup", "import", IgnoreCase = true)]
        public string ContentType { get; set; } = "iso";

        /// <summary>When specified, waits for the download task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        /// <summary>
        /// HTTP timeout for issuing the download request, in seconds. Pass 0 for infinite.
        /// When omitted, defaults to 30 minutes. Note: this only bounds the API call that
        /// schedules the download; the actual file transfer runs server-side and is tracked
        /// by the returned task.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "HTTP timeout in seconds (0 = infinite). Defaults to 1800 (30 min).")]
        [ValidateRange(0, int.MaxValue)]
        public int? TimeoutSeconds { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"{Node}/{Storage}/{Filename}", $"Download from {Url}"))
                return;

            var session = GetSession();
            RequireVersion(session, "Storage URL download", 7, 0);

            TimeSpan timeout;
            if (TimeoutSeconds.HasValue)
            {
                timeout = TimeoutSeconds.Value == 0
                    ? System.Threading.Timeout.InfiniteTimeSpan
                    : TimeSpan.FromSeconds(TimeoutSeconds.Value);
            }
            else
            {
                timeout = TimeSpan.FromMinutes(30);
            }
            using var client = new PveHttpClient(session, timeout);

            WriteVerbose($"Downloading '{Url}' to {Node}/{Storage}...");
            var resource = $"nodes/{Uri.EscapeDataString(Node)}/storage/{Uri.EscapeDataString(Storage)}/download-url";
            var data = new Dictionary<string, string>
            {
                ["url"]      = Url,
                ["filename"] = Filename,
                ["content"]  = ContentType
            };

            var json = client.PostAsync(resource, data).GetAwaiter().GetResult();
            var root = JObject.Parse(json);
            var upid = root["data"]?.ToString() ?? string.Empty;

            var task = new PveTask { Upid = upid, Node = Node, Status = "running" };

            if (Wait.IsPresent && !string.IsNullOrEmpty(upid))
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, upid);
            }

            WriteObject(task);
        }
    }
}
