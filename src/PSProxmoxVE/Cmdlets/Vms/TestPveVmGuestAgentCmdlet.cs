using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Tests whether the QEMU guest agent is responding on a VM.</para>
    /// <para type="description">
    /// Pings the QEMU guest agent on the specified virtual machine.
    /// Returns $true if the agent responds, $false otherwise.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "PveVmGuestAgent")]
    [OutputType(typeof(bool))]
    public sealed class TestPveVmGuestAgentCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose($"Pinging guest agent on VM {VmId}...");
            var service = new VmService();
            WriteObject(service.PingGuestAgent(session, Node, VmId));
        }
    }
}
