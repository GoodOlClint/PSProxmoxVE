using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.CloudInit
{
    /// <summary>
    /// <para type="synopsis">Sets cloud-init configuration on a Proxmox VE virtual machine.</para>
    /// <para type="description">
    /// Updates cloud-init settings for the specified VM. Changes take effect on the next
    /// VM boot after the cloud-init drive is regenerated. Only specified parameters are
    /// updated. Use Invoke-PveCloudInitRegenerate to force drive regeneration.
    /// Returns a PveTask. Use -Wait to block until the update completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveCloudInitConfig", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public class SetPveCloudInitConfigCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier. Accepts pipeline input from Get-PveVm (PveVm.VmId).</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>Cloud-init hostname override.</summary>
        [Parameter(Mandatory = false)]
        public string? Hostname { get; set; }

        /// <summary>Cloud-init default username.</summary>
        [Parameter(Mandatory = false)]
        public string? User { get; set; }

        /// <summary>Cloud-init default user password. Accepts a SecureString.</summary>
        [Parameter(Mandatory = false)]
        public System.Security.SecureString? Password { get; set; }

        /// <summary>SSH public keys to inject (one key per element).</summary>
        [Parameter(Mandatory = false)]
        public string[]? SshKeys { get; set; }

        /// <summary>
        /// IP configuration string (e.g., "ip=192.168.1.50/24,gw=192.168.1.1").
        /// Maps to ipconfig0 on the VM.
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? IpConfig { get; set; }

        /// <summary>DNS nameserver(s) to inject (space or comma separated).</summary>
        [Parameter(Mandatory = false)]
        public string? Dns { get; set; }

        /// <summary>DNS search domain to inject.</summary>
        [Parameter(Mandatory = false)]
        public string? SearchDomain { get; set; }

        /// <summary>When specified, waits for the config update task to complete before returning.</summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on {Node}", "Set PVE Cloud-Init Config"))
                return;

            var config = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(User))         config["ciuser"]       = User;
            if (!string.IsNullOrEmpty(Hostname))     config["name"]         = Hostname;
            if (!string.IsNullOrEmpty(IpConfig))     config["ipconfig0"]    = IpConfig;
            if (!string.IsNullOrEmpty(Dns))          config["nameserver"]   = Dns;
            if (!string.IsNullOrEmpty(SearchDomain)) config["searchdomain"] = SearchDomain;

            if (Password != null)
            {
                var ptr = Marshal.SecureStringToGlobalAllocUnicode(Password);
                try { config["cipassword"] = Marshal.PtrToStringUni(ptr) ?? string.Empty; }
                finally { Marshal.ZeroFreeGlobalAllocUnicode(ptr); }
            }

            if (SshKeys != null && SshKeys.Length > 0)
            {
                // PVE expects URL-encoded newline-delimited keys
                var combined = string.Join("\n", SshKeys);
                config["sshkeys"] = Uri.EscapeDataString(combined);
            }

            if (config.Count == 0)
            {
                WriteWarning("No cloud-init parameters specified. Nothing to update.");
                return;
            }

            var session = GetSession();
            var service = new CloudInitService();
            service.SetCloudInitConfig(session, Node, VmId, config);

            // The PVE config PUT endpoint may or may not return a task UPID;
            // surface a minimal task object so callers always get consistent output.
            var task = new PveTask { Node = Node, Status = "stopped", ExitStatus = "OK" };

            WriteObject(task);
        }
    }
}
