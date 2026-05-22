using System.Collections.Generic;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Creates a new QEMU/KVM virtual machine on a Proxmox VE node.</para>
    /// <para type="description">
    /// Creates a new virtual machine using the Proxmox VE API. Supports common hardware
    /// configuration options. Use -Wait to block until the creation task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveVm", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class NewPveVmCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which to create the VM.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The VM ID to assign. When omitted, the next available ID is used.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        /// <summary>
        /// <para type="description">The display name of the VM.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The display name of the VM.")]
        public string? Name { get; set; }

        /// <summary>
        /// <para type="description">Memory size in MiB.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Memory size in MiB.")]
        public int? Memory { get; set; }

        /// <summary>
        /// <para type="description">Number of CPU cores per socket.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Number of CPU cores per socket.")]
        public int? Cores { get; set; }

        /// <summary>
        /// <para type="description">Number of CPU sockets.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Number of CPU sockets.")]
        public int? Sockets { get; set; }

        /// <summary>
        /// <para type="description">Emulated CPU type (e.g., "host", "x86-64-v2-AES").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Emulated CPU type (e.g. host, x86-64-v2-AES).")]
        public string? CpuType { get; set; }

        /// <summary>
        /// <para type="description">BIOS type: "seabios" (default) or "ovmf" (UEFI).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "BIOS type: seabios (default) or ovmf (UEFI).")]
        public string? Bios { get; set; }

        /// <summary>
        /// <para type="description">Emulated machine type (e.g., "q35", "i440fx").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Emulated machine type (e.g. q35, i440fx).")]
        public string? Machine { get; set; }

        /// <summary>
        /// <para type="description">
        /// Size of the primary disk. Accepts a bare integer in GiB ("32") or a value
        /// suffixed with G/GB/T/TB (case-insensitive); the value is normalized to a
        /// bare GiB count before being sent to the API.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Size of the primary disk in GiB (e.g. 32 or 32G).")]
        public string? DiskSize { get; set; }

        /// <summary>
        /// <para type="description">Storage pool for the primary disk (e.g., "local-lvm").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Storage pool for the primary disk.")]
        public string? DiskStorage { get; set; }

        /// <summary>
        /// <para type="description">Disk format (e.g., "raw", "qcow2").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Disk format (e.g. raw, qcow2).")]
        public string? DiskFormat { get; set; }

        /// <summary>
        /// <para type="description">
        /// Bus/controller for the primary disk: "virtio" (default), "scsi", "sata", or "ide".
        /// Determines the device key (virtio0, scsi0, sata0, ide0).
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Primary disk bus: virtio (default), scsi, sata, ide.")]
        [ValidateSet("virtio", "scsi", "sata", "ide", IgnoreCase = true)]
        public string? DiskBus { get; set; }

        /// <summary>
        /// <para type="description">
        /// SCSI controller hardware model (sets the VM-level "scsihw" key), e.g.
        /// "virtio-scsi-single", "virtio-scsi-pci", "lsi". Required as
        /// "virtio-scsi-single" when combining -DiskBus scsi with -DiskIoThread.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "SCSI controller model (e.g. virtio-scsi-single).")]
        public string? ScsiHardware { get; set; }

        /// <summary>
        /// <para type="description">
        /// Enable a dedicated IO thread for the primary disk (iothread=1). Valid only for
        /// the virtio bus or for the scsi bus with -ScsiHardware virtio-scsi-single.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Enable a dedicated IO thread (iothread=1).")]
        public SwitchParameter DiskIoThread { get; set; }

        /// <summary>
        /// <para type="description">Async IO mode for the primary disk: "native", "threads", or "io_uring".</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Async IO mode: native, threads, io_uring.")]
        [ValidateSet("native", "threads", "io_uring", IgnoreCase = true)]
        public string? DiskAio { get; set; }

        /// <summary>
        /// <para type="description">
        /// Mark the primary disk as SSD (ssd=1). Not supported on the virtio bus — use
        /// -DiskBus scsi, sata, or ide.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Mark the primary disk as SSD (ssd=1).")]
        public SwitchParameter DiskSsd { get; set; }

        /// <summary>
        /// <para type="description">Enable discard/TRIM passthrough on the primary disk (discard=on).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Enable discard/TRIM passthrough (discard=on).")]
        public SwitchParameter DiskDiscard { get; set; }

        /// <summary>
        /// <para type="description">
        /// Cache mode for the primary disk: "none", "writethrough", "writeback",
        /// "directsync", or "unsafe".
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Disk cache mode: none, writethrough, writeback, directsync, unsafe.")]
        [ValidateSet("none", "writethrough", "writeback", "directsync", "unsafe", IgnoreCase = true)]
        public string? DiskCache { get; set; }

        /// <summary>
        /// <para type="description">Network interface model (e.g., "virtio", "e1000").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Network interface model (e.g. virtio, e1000).")]
        public string? Network { get; set; }

        /// <summary>
        /// <para type="description">Network bridge to attach to (e.g., "vmbr0").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Network bridge to attach to (e.g. vmbr0).")]
        public string? Bridge { get; set; }

        /// <summary>
        /// <para type="description">Guest OS type hint (e.g., "l26" for Linux 2.6+, "win10").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Guest OS type hint (e.g. l26, win10).")]
        public string? OsType { get; set; }

        /// <summary>
        /// <para type="description">When specified, starts the VM after creation.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Start the VM after creation.")]
        public SwitchParameter Start { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the creation task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            // Validate -DiskSize and disk-option combinations before ShouldProcess so
            // typos and invalid combos are rejected even with -WhatIf, and so the error
            // is raised regardless of whether -DiskStorage is also supplied.
            string? diskSizeGib = null;
            if (!string.IsNullOrEmpty(DiskSize))
                diskSizeGib = SizeParser.NormalizeToGibibytes(DiskSize!, nameof(DiskSize));

            var diskBus = string.IsNullOrEmpty(DiskBus) ? "virtio" : DiskBus!.ToLowerInvariant();
            ValidateDiskOptions(diskBus);

            if (!ShouldProcess($"VM on node '{Node}'", "New-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            WriteVerbose($"Creating VM on node '{Node}'...");
            var config = new Dictionary<string, object>();

            if (VmId.HasValue)
            {
                config["vmid"] = VmId.Value;
            }
            else
            {
                // Auto-allocate the next available VM ID from the cluster.
                using var allocClient = new PveHttpClient(session);
                var nextIdJson = allocClient.GetAsync("cluster/nextid").GetAwaiter().GetResult();
                var nextIdData = JObject.Parse(nextIdJson)["data"];
                config["vmid"] = int.Parse(nextIdData!.ToString());
            }
            if (!string.IsNullOrEmpty(Name))
                config["name"] = Name!;
            if (Memory.HasValue)
                config["memory"] = Memory.Value;
            if (Cores.HasValue)
                config["cores"] = Cores.Value;
            if (Sockets.HasValue)
                config["sockets"] = Sockets.Value;
            if (!string.IsNullOrEmpty(CpuType))
                config["cpu"] = CpuType!;
            if (!string.IsNullOrEmpty(Bios))
                config["bios"] = Bios!;
            if (!string.IsNullOrEmpty(Machine))
                config["machine"] = Machine!;
            if (!string.IsNullOrEmpty(OsType))
                config["ostype"] = OsType!;
            if (!string.IsNullOrEmpty(ScsiHardware))
                config["scsihw"] = ScsiHardware!;

            if (!string.IsNullOrEmpty(DiskStorage) && diskSizeGib != null)
                config[$"{diskBus}0"] = BuildDiskSpec(DiskStorage!, diskSizeGib);
            else if (HasDiskOptions())
                WriteWarning("Disk IO options (-DiskBus/-DiskIoThread/-DiskAio/-DiskSsd/-DiskDiscard/-DiskCache) "
                    + "were specified but no disk is being created (-DiskStorage and -DiskSize are both required). "
                    + "The options were ignored.");

            if (!string.IsNullOrEmpty(Bridge))
            {
                var netModel = string.IsNullOrEmpty(Network) ? "virtio" : Network!;
                config["net0"] = $"{netModel},bridge={Bridge}";
            }

            if (Start.IsPresent)
                config["start"] = "1";

            var task = vmService.CreateVm(session, Node, config);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }

        private bool HasDiskOptions() =>
            !string.IsNullOrEmpty(DiskBus)
            || DiskIoThread.IsPresent
            || !string.IsNullOrEmpty(DiskAio)
            || DiskSsd.IsPresent
            || DiskDiscard.IsPresent
            || !string.IsNullOrEmpty(DiskCache);

        /// <summary>
        /// Validates disk option combinations that PVE would otherwise reject at VM start
        /// rather than at create time, surfacing a clear error up front.
        /// </summary>
        private void ValidateDiskOptions(string diskBus)
        {
            if (DiskSsd.IsPresent && diskBus == "virtio")
                throw new PSArgumentException(
                    "-DiskSsd is not supported on the virtio bus. Use -DiskBus scsi, sata, or ide.",
                    nameof(DiskSsd));

            if (DiskIoThread.IsPresent)
            {
                if (diskBus == "sata" || diskBus == "ide")
                    throw new PSArgumentException(
                        "-DiskIoThread requires -DiskBus virtio or scsi.",
                        nameof(DiskIoThread));

                if (diskBus == "scsi"
                    && !string.Equals(ScsiHardware, "virtio-scsi-single", System.StringComparison.OrdinalIgnoreCase))
                    throw new PSArgumentException(
                        "-DiskIoThread on a scsi disk requires -ScsiHardware virtio-scsi-single.",
                        nameof(DiskIoThread));
            }
        }

        /// <summary>
        /// Builds the disk volume spec ("&lt;storage&gt;:&lt;sizeGiB&gt;[,opt=val...]") for the
        /// primary disk, appending format and any requested IO options in a stable order.
        /// </summary>
        private string BuildDiskSpec(string storage, string sizeGib)
        {
            var sb = new System.Text.StringBuilder($"{storage}:{sizeGib}");
            if (!string.IsNullOrEmpty(DiskFormat))
                sb.Append($",format={DiskFormat}");
            if (!string.IsNullOrEmpty(DiskCache))
                sb.Append($",cache={DiskCache!.ToLowerInvariant()}");
            if (!string.IsNullOrEmpty(DiskAio))
                sb.Append($",aio={DiskAio!.ToLowerInvariant()}");
            if (DiskSsd.IsPresent)
                sb.Append(",ssd=1");
            if (DiskDiscard.IsPresent)
                sb.Append(",discard=on");
            if (DiskIoThread.IsPresent)
                sb.Append(",iothread=1");
            return sb.ToString();
        }
    }
}
