using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Imports an OVA file into a Proxmox VE virtual machine.</para>
    /// <para type="description">
    /// Imports an OVA (Open Virtual Appliance) archive into Proxmox VE by:
    /// 1. Parsing the embedded OVF descriptor to extract VM configuration (CPU, memory, disks, networks)
    /// 2. Uploading the OVA to PVE storage with content=import
    /// 3. Creating a new VM with the extracted (or overridden) configuration
    /// 4. Importing each VMDK disk from the OVA into the target storage
    /// 5. Configuring network adapters
    ///
    /// Use -Name, -Memory, or -Cores to override the values discovered in the OVF.
    /// Use -Wait to block until all import tasks complete.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsData.Import, "PveOva", SupportsShouldProcess = true)]
    [OutputType(typeof(PveVm))]
    public sealed class ImportPveOvaCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The Proxmox VE node to import the OVA on.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The storage to upload the OVA file to (must support 'import' content type).</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The storage pool for OVA upload.")]
        public string Storage { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The local path to the OVA file to import.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "Local path to the OVA file.")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The target storage for imported VM disks (e.g. "local-lvm").</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Target storage for imported VM disks.")]
        public string TargetStorage { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The VM ID to assign. When omitted, the next available ID is auto-assigned.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The VM identifier. Auto-assigned if omitted.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        /// <summary>
        /// <para type="description">Override the VM name from the OVF descriptor.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Override the VM name from the OVF.")]
        public string? Name { get; set; }

        /// <summary>
        /// <para type="description">Override the memory size (in MiB) from the OVF descriptor.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Override memory size in MiB.")]
        public int? Memory { get; set; }

        /// <summary>
        /// <para type="description">Override the CPU core count from the OVF descriptor.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Override CPU core count.")]
        public int? Cores { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for all tasks to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for all tasks to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            // Validate the OVA file exists
            if (!File.Exists(Path))
            {
                ThrowTerminatingError(new ErrorRecord(
                    new FileNotFoundException($"OVA file not found: {Path}"),
                    "OvaFileNotFound",
                    ErrorCategory.ObjectNotFound,
                    Path));
                return;
            }

            // Step 1: Parse OVA to extract OVF metadata
            WriteVerbose("Parsing OVA file to extract OVF metadata...");
            OvfMetadata metadata;
            try
            {
                metadata = OvfMetadata.FromOva(Path);
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(
                    ex,
                    "OvfParseError",
                    ErrorCategory.ParserError,
                    Path));
                return;
            }

            // Step 2: Log discovered configuration
            var vmName = Name ?? metadata.Name;
            var vmMemory = Memory ?? metadata.MemoryMB;
            var vmCores = Cores ?? metadata.CpuCount;

            WriteVerbose($"OVF VM Name: {metadata.Name}");
            WriteVerbose($"OVF CPU Cores: {metadata.CpuCount}");
            WriteVerbose($"OVF Memory: {metadata.MemoryMB} MB");
            WriteVerbose($"OVF OS Type: {metadata.OsTypeHint}");
            WriteVerbose($"OVF Disks: {metadata.Disks.Count}");
            foreach (var disk in metadata.Disks)
            {
                WriteVerbose($"  Disk: {disk.FileName} (bus: {disk.BusType})");
            }
            WriteVerbose($"OVF Network Adapters: {metadata.NetworkAdapters.Count}");
            foreach (var nic in metadata.NetworkAdapters)
            {
                WriteVerbose($"  NIC: {nic.AdapterName} -> {nic.ConnectionName}");
            }

            if (!ShouldProcess($"OVA '{System.IO.Path.GetFileName(Path)}' on node '{Node}'", "Import-PveOva"))
                return;

            var session = GetSession();
            RequireVersion(session, "VM disk import", 8, 1);
            var vmService = new VmService();
            var taskService = new TaskService();

            // Step 3: Auto-assign VM ID if not specified
            int vmId;
            if (VmId.HasValue)
            {
                vmId = VmId.Value;
            }
            else
            {
                using var allocClient = new PveHttpClient(session);
                var nextIdJson = allocClient.GetAsync("cluster/nextid").GetAwaiter().GetResult();
                var nextIdData = JObject.Parse(nextIdJson)["data"];
                vmId = int.Parse(nextIdData!.ToString());
                WriteVerbose($"Auto-assigned VM ID: {vmId}");
            }

            // Step 4: Upload OVA to storage with content=import
            var fileName = System.IO.Path.GetFileName(Path);
            var totalBytes = new FileInfo(Path).Length;

            WriteVerbose($"Uploading OVA to {Node}/{Storage} (content=import)...");

            var activityId = 1;
            var progressRecord = new ProgressRecord(activityId,
                $"Importing OVA to {Node}",
                $"Uploading {fileName}...");

            long progressBytes = 0;
            var uploadTask = System.Threading.Tasks.Task.Run(() =>
                vmService.UploadOva(session, Node, Storage, Path,
                    (bytesSent, _) =>
                        System.Threading.Interlocked.Exchange(ref progressBytes, bytesSent)));

            // Poll progress on the pipeline thread
            while (!uploadTask.IsCompleted)
            {
                System.Threading.Thread.Sleep(500);
                if (totalBytes > 0)
                {
                    var sent = System.Threading.Interlocked.Read(ref progressBytes);
                    progressRecord.PercentComplete = (int)((sent * 100L) / totalBytes);
                    progressRecord.StatusDescription =
                        $"Uploading: {sent / 1024 / 1024} MB / {totalBytes / 1024 / 1024} MB";
                    WriteProgress(progressRecord);
                }
            }

            var uploadResult = uploadTask.GetAwaiter().GetResult();

            progressRecord.RecordType = ProgressRecordType.Completed;
            WriteProgress(progressRecord);

            // Wait for upload task to complete on PVE side
            if (!string.IsNullOrEmpty(uploadResult.Upid))
            {
                WriteVerbose("Waiting for OVA upload task to complete on PVE...");
                var completedUpload = taskService.WaitForTask(session, Node, uploadResult.Upid, null, null, null);
                if (completedUpload.ExitStatus != null && completedUpload.ExitStatus != "OK")
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException($"OVA upload task failed with status: {completedUpload.ExitStatus}"),
                        "OvaUploadFailed",
                        ErrorCategory.InvalidResult,
                        uploadResult.Upid));
                    return;
                }
            }

            // Step 5: Create VM with disks and network in a single API call.
            // PVE handles OVA disk extraction + import as part of qmcreate when
            // import-from is specified in the create parameters.
            WriteVerbose($"Creating VM {vmId} with disk import on node '{Node}'...");
            var vmConfig = new Dictionary<string, object>
            {
                ["vmid"] = vmId
            };

            if (!string.IsNullOrEmpty(vmName))
                vmConfig["name"] = vmName;
            vmConfig["memory"] = vmMemory;
            vmConfig["cores"] = vmCores;
            if (!string.IsNullOrEmpty(metadata.OsTypeHint) && metadata.OsTypeHint != "other")
                vmConfig["ostype"] = metadata.OsTypeHint;

            // Add disk import-from parameters (use scsi bus like PVE UI)
            string? firstDisk = null;
            for (int i = 0; i < metadata.Disks.Count; i++)
            {
                var disk = metadata.Disks[i];
                var diskSlot = $"scsi{i}";
                var importFrom = $"{Storage}:import/{fileName}/{disk.FileName}";
                vmConfig[diskSlot] = $"{TargetStorage}:0,import-from={importFrom}";
                firstDisk ??= diskSlot;
                WriteVerbose($"  Disk {diskSlot}: import-from={importFrom} -> {TargetStorage}");
            }

            // Set boot order to the first disk
            if (firstDisk != null)
                vmConfig["boot"] = $"order={firstDisk}";

            // Set sockets (PVE default)
            vmConfig["sockets"] = 1;

            // Add network adapters with model from OVF
            for (int i = 0; i < metadata.NetworkAdapters.Count; i++)
            {
                var nicModel = OvfMetadata.MapNicModel(metadata.NetworkAdapters[i].ResourceSubType);
                vmConfig[$"net{i}"] = $"{nicModel},bridge=vmbr0";
            }

            var createTask = vmService.CreateVm(session, Node, vmConfig);

            if (Wait.IsPresent && !string.IsNullOrEmpty(createTask.Upid))
            {
                WriteVerbose("Waiting for VM creation + disk import to complete...");
                var completedCreate = taskService.WaitForTask(session, Node, createTask.Upid, null, null, null);
                if (completedCreate.ExitStatus != null && completedCreate.ExitStatus != "OK")
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException($"OVA import task failed with status: {completedCreate.ExitStatus}"),
                        "OvaImportFailed",
                        ErrorCategory.InvalidResult,
                        createTask.Upid));
                    return;
                }
            }

            // Step 8: Output the created VM
            WriteVerbose("Retrieving created VM...");
            try
            {
                var vm = vmService.GetVm(session, Node, vmId);
                WriteObject(vm);
            }
            catch
            {
                // If the VM isn't queryable yet (e.g. disk import still running), return basic info
                WriteObject(new PveVm
                {
                    VmId = vmId,
                    Name = vmName,
                    Node = Node,
                    Status = "stopped"
                });
            }
        }
    }
}
