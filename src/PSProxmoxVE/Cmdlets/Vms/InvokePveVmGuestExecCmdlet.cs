using System;
using System.Diagnostics;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Executes a command inside a VM via the QEMU guest agent.</para>
    /// <para type="description">
    /// Sends a command to the QEMU guest agent running inside the specified VM for execution.
    /// Returns the result including stdout, stderr, and exit code. The guest agent must be
    /// installed and running inside the VM.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "PveVmGuestExec", SupportsShouldProcess = true)]
    [OutputType(typeof(PSObject))]
    public sealed class InvokePveVmGuestExecCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>The command to execute inside the guest.</summary>
        [Parameter(Mandatory = true, Position = 2, HelpMessage = "The command to execute inside the guest.")]
        public string Command { get; set; } = string.Empty;

        /// <summary>Optional arguments to pass to the command.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Arguments to pass to the command.")]
        public string[]? Args { get; set; }

        /// <summary>Maximum time in seconds to wait for the command to complete. Defaults to 300 (5 minutes).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Timeout in seconds to wait for command completion. Defaults to 300.")]
        [ValidateRange(1, 3600)]
        public int Timeout { get; set; } = 300;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", $"Execute guest command: {Command}"))
                return;

            var session = GetSession();
            var service = new VmService();

            WriteVerbose($"Executing command on VM {VmId} via guest agent...");
            var pid = service.ExecuteGuestCommand(session, Node, VmId, Command, Args);

            // Poll for completion with timeout
            var sw = Stopwatch.StartNew();
            var deadline = TimeSpan.FromSeconds(Timeout);
            Newtonsoft.Json.Linq.JObject result;
            do
            {
                System.Threading.Thread.Sleep(1000);
                if (sw.Elapsed >= deadline)
                    throw new TimeoutException($"Guest command did not complete within {Timeout} seconds.");
                result = service.GetGuestExecStatus(session, Node, VmId, pid);
            } while (result["exited"]?.ToObject<int>() != 1);

            var output = new PSObject();
            output.Properties.Add(new PSNoteProperty("ExitCode", result["exitcode"]?.ToObject<int>() ?? -1));
            output.Properties.Add(new PSNoteProperty("Stdout", DecodeBase64(result["out-data"]?.ToString())));
            output.Properties.Add(new PSNoteProperty("Stderr", DecodeBase64(result["err-data"]?.ToString())));
            output.Properties.Add(new PSNoteProperty("Pid", pid));

            WriteObject(output);
        }

        private static string DecodeBase64(string? encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return string.Empty;
            try
            {
                var bytes = System.Convert.FromBase64String(encoded);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return encoded!;
            }
        }
    }
}
