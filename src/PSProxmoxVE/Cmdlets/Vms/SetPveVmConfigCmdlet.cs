using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Updates the configuration of a QEMU/KVM virtual machine.</para>
    /// <para type="description">
    /// Modifies one or more configuration settings of the specified virtual machine via the
    /// Proxmox VE API. Only the parameters explicitly provided are changed; all other settings
    /// are left untouched. The operation is synchronous and produces no output.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveVmConfig", SupportsShouldProcess = true)]
    public sealed class SetPveVmConfigCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the VM resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM to configure. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

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
        /// <para type="description">Memory size in MiB.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Memory size in MiB.")]
        public int? Memory { get; set; }

        /// <summary>
        /// <para type="description">Emulated CPU type (e.g., "host", "x86-64-v2-AES").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Emulated CPU type (e.g. host, x86-64-v2-AES).")]
        public string? CpuType { get; set; }

        /// <summary>
        /// <para type="description">Human-readable description / notes for the VM.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Description or notes for the VM.")]
        public string? Description { get; set; }

        /// <summary>
        /// <para type="description">Semicolon-separated list of tags to assign to the VM.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Semicolon-separated list of tags.")]
        public string? Tags { get; set; }

        /// <summary>
        /// <para type="description">BIOS type: "seabios" or "ovmf".</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "BIOS type: seabios or ovmf.")]
        public string? Bios { get; set; }

        /// <summary>
        /// <para type="description">Emulated machine type (e.g., "q35", "i440fx").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Emulated machine type (e.g. q35, i440fx).")]
        public string? Machine { get; set; }

        /// <summary>
        /// <para type="description">Guest OS type hint (e.g., "l26", "win10").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Guest OS type hint (e.g. l26, win10).")]
        public string? OsType { get; set; }

        /// <summary>
        /// <para type="description">
        /// Hashtable of additional configuration keys to set. Use this for any PVE config
        /// option not exposed as a named parameter (e.g., scsi0, boot, agent, net0, ide2).
        /// Values are merged after named parameters and can override them.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Extra config keys as a hashtable.")]
        public Hashtable? AdditionalConfig { get; set; }

        /// <summary>
        /// <para type="description">
        /// Comma-separated list of configuration keys to delete (e.g., "ide2,args").
        /// Maps to the PVE API "delete" parameter.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated config keys to delete.")]
        public string? Delete { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", "Set-PveVmConfig"))
                return;

            var session = GetSession();
            var vmService = new VmService();

            WriteVerbose($"Updating config for VM {VmId} on node '{Node}'...");
            var config = new Dictionary<string, object>();

            if (Cores.HasValue)
                config["cores"] = Cores.Value;
            if (Sockets.HasValue)
                config["sockets"] = Sockets.Value;
            if (Memory.HasValue)
                config["memory"] = Memory.Value;
            if (!string.IsNullOrEmpty(CpuType))
                config["cpu"] = CpuType!;
            if (!string.IsNullOrEmpty(Description))
                config["description"] = Description!;
            if (!string.IsNullOrEmpty(Tags))
                config["tags"] = Tags!;
            if (!string.IsNullOrEmpty(Bios))
                config["bios"] = Bios!;
            if (!string.IsNullOrEmpty(Machine))
                config["machine"] = Machine!;
            if (!string.IsNullOrEmpty(OsType))
                config["ostype"] = OsType!;

            if (AdditionalConfig != null)
            {
                foreach (DictionaryEntry entry in AdditionalConfig)
                    config[entry.Key.ToString()!] = entry.Value ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(Delete))
                config["delete"] = Delete!;

            vmService.SetVmConfig(session, Node, VmId, config);
        }
    }
}
