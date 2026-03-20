using System;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Applies pending network configuration changes on a Proxmox VE node.</para>
    /// <para type="description">
    /// Reloads the network configuration on the specified node, applying any pending changes
    /// made via New-PveNetwork, Set-PveNetwork, or Remove-PveNetwork.
    /// Returns a PveTask. Use -Wait to block until the apply completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "PveNetworkApply", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public class InvokePveNetworkApplyCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node on which to apply network changes.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>When specified, waits for the apply task to complete before returning.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Node, "Apply PVE Network Configuration"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            WriteVerbose($"Applying network configuration on node '{Node}'...");
            var json = client.PutAsync($"nodes/{Node}/network").GetAwaiter().GetResult();
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
