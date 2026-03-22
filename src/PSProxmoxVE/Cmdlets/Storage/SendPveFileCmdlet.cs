using System;
using System.IO;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Uploads a local file to a Proxmox VE storage.</para>
    /// <para type="description">
    /// Uploads a file from the local filesystem to the specified node/storage using
    /// the Proxmox VE upload API. Supports ISO images, container templates, and
    /// disk images for import. Streams the file in chunks and reports progress
    /// via Write-Progress. Returns a PveTask representing the upload job.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "PveFile", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class SendPveFileCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node to upload to.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The target storage identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The storage pool name.")]
        public string Storage { get; set; } = string.Empty;

        /// <summary>
        /// The full local path to the file to upload. The file must exist.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "Local path to the file to upload.")]
        [FileExistsValidation]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The PVE storage content type. Determines where the file is stored:
        /// "iso" for ISO images, "vztmpl" for container templates,
        /// "import" for disk images and OVA files to be imported into VMs.
        /// </summary>
        [Parameter(Mandatory = false, Position = 3, HelpMessage = "Content type: iso, vztmpl, or import.")]
        [ValidateSet("iso", "vztmpl", "import", IgnoreCase = true)]
        public string ContentType { get; set; } = "iso";

        /// <summary>Optional checksum value to verify the uploaded file.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Checksum value to verify the upload.")]
        public string? Checksum { get; set; }

        /// <summary>Checksum algorithm used for verification.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Checksum algorithm (md5, sha1, sha256, sha512).")]
        [ValidateSet("md5", "sha1", "sha256", "sha512", IgnoreCase = true)]
        public string? ChecksumAlgorithm { get; set; }

        /// <summary>
        /// When specified, waits for the upload task to complete before returning.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            var fileName = System.IO.Path.GetFileName(Path);
            if (!ShouldProcess($"{Node}/{Storage}/{fileName}", $"Upload file (content={ContentType})"))
                return;

            var session = GetSession();

            if ((!string.IsNullOrEmpty(Checksum) || !string.IsNullOrEmpty(ChecksumAlgorithm))
                && session.ServerVersion != null && !session.ServerVersion.IsAtLeast(7, 1))
            {
                WriteWarning("The -Checksum and -ChecksumAlgorithm parameters require PVE 7.1 or later. "
                    + $"Connected server is PVE {session.ServerVersion}. The upload will proceed without checksum verification.");
            }

            using var client = new PveHttpClient(session);

            WriteVerbose($"Uploading {fileName} to {Node}/{Storage} (content={ContentType})...");
            var resource = $"nodes/{Node}/storage/{Storage}/upload";
            var totalBytes = new System.IO.FileInfo(Path).Length;

            var activityId = 1;
            var progressRecord = new ProgressRecord(activityId,
                $"Uploading to {Node}/{Storage}",
                $"Uploading {fileName}...");

            // Track progress via an atomic counter updated from the upload thread.
            // WriteProgress must be called from the pipeline thread — passing the
            // callback directly would invoke it from the HTTP serialization thread
            // and cause PowerShell to throw an InvalidOperationException mid-upload.
            long progressBytes = 0;
            var uploadTask = System.Threading.Tasks.Task.Run(() =>
                client.UploadFileAsync(
                    resource,
                    Path,
                    formFields: new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["content"] = ContentType
                    },
                    checksum: Checksum,
                    checksumAlgorithm: ChecksumAlgorithm,
                    progressCallback: (bytesSent, _) =>
                        System.Threading.Interlocked.Exchange(ref progressBytes, bytesSent)));

            // Poll progress on the pipeline thread while the upload runs.
            while (!uploadTask.IsCompleted)
            {
                System.Threading.Thread.Sleep(500);
                if (totalBytes > 0)
                {
                    var sent = System.Threading.Interlocked.Read(ref progressBytes);
                    progressRecord.PercentComplete = (int)((sent * 100L) / totalBytes);
                    progressRecord.StatusDescription =
                        $"{sent / 1024 / 1024} MB / {totalBytes / 1024 / 1024} MB";
                    WriteProgress(progressRecord);
                }
            }

            var json = uploadTask.GetAwaiter().GetResult();

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
            var statusResource = $"nodes/{node}/tasks/{encodedUpid}/status";

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
