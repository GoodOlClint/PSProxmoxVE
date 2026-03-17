using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

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
        [Parameter(Mandatory = true, Position = 0)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The VM ID to assign. When omitted, the next available ID is used.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? VmId { get; set; }

        /// <summary>
        /// <para type="description">The display name of the VM.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Name { get; set; }

        /// <summary>
        /// <para type="description">Memory size in MiB.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? Memory { get; set; }

        /// <summary>
        /// <para type="description">Number of CPU cores per socket.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? Cores { get; set; }

        /// <summary>
        /// <para type="description">Number of CPU sockets.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? Sockets { get; set; }

        /// <summary>
        /// <para type="description">Emulated CPU type (e.g., "host", "x86-64-v2-AES").</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? CpuType { get; set; }

        /// <summary>
        /// <para type="description">BIOS type: "seabios" (default) or "ovmf" (UEFI).</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Bios { get; set; }

        /// <summary>
        /// <para type="description">Emulated machine type (e.g., "q35", "i440fx").</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Machine { get; set; }

        /// <summary>
        /// <para type="description">Size of the primary disk (e.g., "32G").</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? DiskSize { get; set; }

        /// <summary>
        /// <para type="description">Storage pool for the primary disk (e.g., "local-lvm").</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? DiskStorage { get; set; }

        /// <summary>
        /// <para type="description">Disk format (e.g., "raw", "qcow2").</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? DiskFormat { get; set; }

        /// <summary>
        /// <para type="description">Network interface model (e.g., "virtio", "e1000").</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Network { get; set; }

        /// <summary>
        /// <para type="description">Network bridge to attach to (e.g., "vmbr0").</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Bridge { get; set; }

        /// <summary>
        /// <para type="description">Guest OS type hint (e.g., "l26" for Linux 2.6+, "win10").</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? OsType { get; set; }

        /// <summary>
        /// <para type="description">When specified, starts the VM after creation.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Start { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the creation task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM on node '{Node}'", "New-PveVm"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            var config = new Dictionary<string, object>();

            if (VmId.HasValue)
                config["vmid"] = VmId.Value;
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

            if (!string.IsNullOrEmpty(DiskStorage) && !string.IsNullOrEmpty(DiskSize))
            {
                var diskValue = $"{DiskStorage}:{DiskSize}";
                if (!string.IsNullOrEmpty(DiskFormat))
                    diskValue += $",format={DiskFormat}";
                config["virtio0"] = diskValue;
            }

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
                task = taskService.WaitForTask(session, task.Node, task.Upid, null, null, null);
            }

            WriteObject(task);
        }
    }
}
