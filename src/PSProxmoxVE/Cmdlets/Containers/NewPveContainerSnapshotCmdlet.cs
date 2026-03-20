using System;
using System.Collections.Generic;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Creates a snapshot of a Proxmox VE container.</para>
    /// <para type="description">
    /// Takes a snapshot of the specified LXC container.
    /// Returns a PveTask. Use -Wait to block until the snapshot completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveContainerSnapshot", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public class NewPveContainerSnapshotCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The container identifier. Accepts pipeline input from Get-PveContainer (PveContainer.VmId).</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>The snapshot name (alphanumeric, hyphens and underscores).</summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "The snapshot name.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional human-readable description for the snapshot.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Description for the snapshot.")]
        public string? Description { get; set; }

        /// <summary>When specified, waits for the snapshot task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on {Node}", $"Create snapshot '{Name}'"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            WriteVerbose($"Creating snapshot '{Name}' for container {VmId}...");
            var data = new Dictionary<string, string>
            {
                ["snapname"] = Name
            };
            if (!string.IsNullOrEmpty(Description)) data["description"] = Description!;

            var json = client.PostAsync($"nodes/{Node}/lxc/{VmId}/snapshot", data).GetAwaiter().GetResult();
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
                var d = statusRoot["data"];
                if (d?["status"]?.ToString() == "stopped")
                    return new PveTask { Upid = upid, Node = node, Status = "stopped", ExitStatus = d["exitstatus"]?.ToString() };
            }
        }
    }
}
