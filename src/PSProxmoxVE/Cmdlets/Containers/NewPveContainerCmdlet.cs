using System.Collections.Generic;
using System.Management.Automation;
using System.Net;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Creates a new LXC container on a Proxmox VE node.</para>
    /// <para type="description">
    /// Creates a new Linux container using the Proxmox VE API. An OS template is required.
    /// The container runs in unprivileged mode by default. Use -Wait to block until the
    /// creation task completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveContainer", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class NewPveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which to create the container.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The container ID to assign. When omitted, the next available ID is used.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        /// <summary>
        /// <para type="description">The hostname to assign to the container.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The hostname for the container.")]
        public string? Hostname { get; set; }

        /// <summary>
        /// <para type="description">Memory limit in MiB.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Memory limit in MiB.")]
        public int? Memory { get; set; }

        /// <summary>
        /// <para type="description">Swap size in MiB.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Swap size in MiB.")]
        public int? Swap { get; set; }

        /// <summary>
        /// <para type="description">Number of CPU cores to allocate to the container.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Number of CPU cores to allocate.")]
        public int? Cores { get; set; }

        /// <summary>
        /// <para type="description">
        /// Size of the root filesystem. Accepts a bare integer in GiB ("8") or a value
        /// suffixed with G/GB/T/TB (case-insensitive); the value is normalized to a
        /// bare GiB count before being sent to the API.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Size of the root filesystem in GiB (e.g. 8 or 8G).")]
        public string? RootFsSize { get; set; }

        /// <summary>
        /// <para type="description">Storage pool for the root filesystem (e.g., "local-lvm").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Storage pool for the root filesystem.")]
        public string? RootFsStorage { get; set; }

        /// <summary>
        /// <para type="description">
        /// The OS template to use (e.g., "local:vztmpl/ubuntu-22.04-standard_22.04-1_amd64.tar.zst").
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "OS template to use for the container.")]
        public string? OsTemplate { get; set; }

        /// <summary>
        /// <para type="description">Root password for the container.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Root password for the container.")]
        public System.Security.SecureString? Password { get; set; }

        /// <summary>
        /// <para type="description">SSH public keys to inject for the root user (newline-separated).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "SSH public keys for the root user.")]
        public string? SshPublicKeys { get; set; }

        /// <summary>
        /// <para type="description">
        /// When specified (the default), the container runs in unprivileged mode.
        /// To create a privileged container, explicitly set this to $false.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Run in unprivileged mode (default true).")]
        public SwitchParameter Unprivileged { get; set; } = new SwitchParameter(true);

        /// <summary>
        /// <para type="description">Network interface model (e.g., "eth0").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Network interface name (e.g. eth0).")]
        public string? Network { get; set; }

        /// <summary>
        /// <para type="description">Network bridge to attach to (e.g., "vmbr0").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Network bridge to attach to (e.g. vmbr0).")]
        public string? Bridge { get; set; }

        /// <summary>
        /// <para type="description">When specified, starts the container after creation.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Start the container after creation.")]
        public SwitchParameter Start { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the creation task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container on node '{Node}'", "New-PveContainer"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Creating container on node '{Node}'...");
            var config = new Dictionary<string, object>();

            if (VmId.HasValue)
            {
                config["vmid"] = VmId.Value.ToString();
            }
            else
            {
                // Auto-allocate the next available ID from the cluster.
                using var allocClient = new PveHttpClient(session);
                var nextIdJson = allocClient.GetAsync("cluster/nextid").GetAwaiter().GetResult();
                var nextIdData = JObject.Parse(nextIdJson)["data"];
                config["vmid"] = int.Parse(nextIdData!.ToString());
            }
            if (!string.IsNullOrEmpty(Hostname))
                config["hostname"] = Hostname!;
            if (Memory.HasValue)
                config["memory"] = Memory.Value.ToString();
            if (Swap.HasValue)
                config["swap"] = Swap.Value.ToString();
            if (Cores.HasValue)
                config["cores"] = Cores.Value.ToString();
            if (!string.IsNullOrEmpty(OsTemplate))
                config["ostemplate"] = OsTemplate!;

            if (!string.IsNullOrEmpty(RootFsStorage))
            {
                var rootFsValue = RootFsStorage!;
                if (!string.IsNullOrEmpty(RootFsSize))
                {
                    var sizeGib = SizeParser.NormalizeToGibibytes(RootFsSize!, nameof(RootFsSize));
                    rootFsValue += $":{sizeGib}";
                }
                config["rootfs"] = rootFsValue;
            }

            if (Password != null)
            {
                var ptr = Marshal.SecureStringToGlobalAllocUnicode(Password);
                try
                {
                    config["password"] = Marshal.PtrToStringUni(ptr) ?? string.Empty;
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }

            if (!string.IsNullOrEmpty(SshPublicKeys))
                config["ssh-public-keys"] = SshPublicKeys!;

            config["unprivileged"] = Unprivileged.IsPresent ? "1" : "0";

            if (!string.IsNullOrEmpty(Bridge))
            {
                var ifName = string.IsNullOrEmpty(Network) ? "eth0" : Network!;
                config["net0"] = $"name={ifName},bridge={Bridge}";
            }

            if (Start.IsPresent)
                config["start"] = "1";

            var task = containerService.CreateContainer(session, Node, config);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, task.Node ?? Node, task.Upid!, null, null, null);
            }

            WriteObject(task);
        }
    }
}
