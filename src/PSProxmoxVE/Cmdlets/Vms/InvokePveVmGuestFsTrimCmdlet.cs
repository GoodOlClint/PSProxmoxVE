using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Invokes an fstrim operation inside the guest via the QEMU guest agent.</para>
    /// <para type="description">
    /// Triggers an fstrim (filesystem trim) operation inside the guest operating system
    /// using the QEMU guest agent. This reclaims unused space on thin-provisioned storage.
    /// The guest agent must be installed and running inside the VM.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "PveVmGuestFsTrim", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class InvokePveVmGuestFsTrimCmdlet : PveCmdletBase
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
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", "Invoke-PveVmGuestFsTrim"))
                return;

            var session = GetSession();

            WriteVerbose($"Invoking fstrim on VM {VmId} via guest agent...");
            var service = new VmService();
            service.GuestFsTrim(session, Node, VmId);
        }
    }
}
