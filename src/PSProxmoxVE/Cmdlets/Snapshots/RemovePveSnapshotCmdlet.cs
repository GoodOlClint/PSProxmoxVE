using System;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Snapshots
{
    /// <summary>
    /// <para type="synopsis">Removes a snapshot from a Proxmox VE virtual machine.</para>
    /// <para type="description">
    /// Deletes the specified snapshot from the VM. Snapshot name can be piped from Get-PveSnapshot.
    /// Returns a PveTask. Use -Wait to block until removal completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveSnapshot", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class RemovePveSnapshotCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// The snapshot name to remove. Accepts pipeline input from Get-PveSnapshot (PveSnapshot.Name).
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, ValueFromPipelineByPropertyName = true, HelpMessage = "The snapshot name to remove.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>When specified, waits for the removal task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            if (!ShouldProcess($"VM {VmId} snapshot '{Name}' on {Node}", "Remove PVE Snapshot"))
                return;

            WriteVerbose($"Removing snapshot '{Name}' from VM {VmId}...");
            using var client = new PveHttpClient(session);

            var json = client.DeleteAsync($"nodes/{Uri.EscapeDataString(Node)}/qemu/{VmId}/snapshot/{Uri.EscapeDataString(Name)}").GetAwaiter().GetResult();
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
