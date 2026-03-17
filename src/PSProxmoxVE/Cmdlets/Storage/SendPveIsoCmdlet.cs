using System;
using System.IO;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Uploads a local ISO file to a Proxmox VE storage.</para>
    /// <para type="description">
    /// Uploads an ISO image from the local filesystem to the specified node/storage using
    /// the Proxmox VE upload API. Streams the file in 4 MB chunks and reports progress
    /// via Write-Progress. Returns a PveTask representing the upload job.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "PveIso", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public class SendPveIsoCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node to upload to.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Node { get; set; } = string.Empty;

        /// <summary>The target storage identifier (must support "iso" content).</summary>
        [Parameter(Mandatory = true, Position = 1)]
        public string Storage { get; set; } = string.Empty;

        /// <summary>
        /// The full local path to the ISO file to upload. The file must exist.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2)]
        [FileExistsValidation]
        public string Path { get; set; } = string.Empty;

        /// <summary>Optional checksum value to verify the uploaded file.</summary>
        [Parameter(Mandatory = false)]
        public string? Checksum { get; set; }

        /// <summary>Checksum algorithm used for verification.</summary>
        [Parameter(Mandatory = false)]
        [ValidateSet("md5", "sha1", "sha256", "sha512", IgnoreCase = true)]
        public string? ChecksumAlgorithm { get; set; }

        /// <summary>
        /// When specified, waits for the upload task to complete before returning.
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            var fileName = System.IO.Path.GetFileName(Path);
            if (!ShouldProcess($"{Node}/{Storage}/{fileName}", "Upload ISO"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            var resource = $"/nodes/{Node}/storage/{Storage}/upload";

            var activityId = 1;
            var progressRecord = new ProgressRecord(activityId,
                $"Uploading ISO to {Node}/{Storage}",
                $"Uploading {fileName}...");

            var json = client.UploadFileAsync(
                resource,
                Path,
                formFields: new System.Collections.Generic.Dictionary<string, string>
                {
                    ["content"] = "iso"
                },
                checksum: Checksum,
                checksumAlgorithm: ChecksumAlgorithm,
                progressCallback: (bytesSent, total) =>
                {
                    if (total > 0)
                    {
                        var pct = (int)((bytesSent * 100L) / total);
                        progressRecord.PercentComplete = pct;
                        progressRecord.StatusDescription = $"{bytesSent / 1024 / 1024} MB / {total / 1024 / 1024} MB";
                        WriteProgress(progressRecord);
                    }
                }
            ).GetAwaiter().GetResult();

            progressRecord.RecordType = ProgressRecordType.Completed;
            WriteProgress(progressRecord);

            var root = JObject.Parse(json);
            var upid = root["data"]?.ToString() ?? string.Empty;

            var task = new PveTask { Upid = upid, Node = Node, Status = "running" };

            if (Wait.IsPresent && !string.IsNullOrEmpty(upid))
            {
                task = WaitForTask(client, Node, upid);
            }

            WriteObject(task);
        }

        private static PveTask WaitForTask(PveHttpClient client, string node, string upid)
        {
            var encodedUpid = Uri.EscapeDataString(upid);
            var statusResource = $"/nodes/{node}/tasks/{encodedUpid}/status";

            while (true)
            {
                System.Threading.Thread.Sleep(2000);
                var statusJson = client.GetAsync(statusResource).GetAwaiter().GetResult();
                var statusRoot = JObject.Parse(statusJson);
                var data = statusRoot["data"];
                var status = data?["status"]?.ToString();
                var exitStatus = data?["exitstatus"]?.ToString();

                if (status == "stopped")
                {
                    return new PveTask
                    {
                        Upid       = upid,
                        Node       = node,
                        Status     = status,
                        ExitStatus = exitStatus
                    };
                }
            }
        }
    }

    /// <summary>Validates that a file path exists on disk.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal sealed class FileExistsValidationAttribute : ValidateArgumentsAttribute
    {
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            var path = arguments as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new ValidationMetadataException($"File not found: {path}");
        }
    }
}
