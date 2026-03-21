using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Writes content to a file on the guest filesystem via the QEMU guest agent.</para>
    /// <para type="description">
    /// Writes the specified content to a file inside the guest operating system using the QEMU guest agent.
    /// The guest agent must be installed and running inside the VM.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "PveVmGuestFile", SupportsShouldProcess = true)]
    public sealed class WritePveVmGuestFileCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The path to the file inside the guest to write.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The file path inside the guest to write.")]
        public string File { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The content to write to the file.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The content to write to the file.")]
        public string Content { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"File '{File}' on VM {VmId} (node '{Node}')", "Write-PveVmGuestFile"))
                return;

            var session = GetSession();

            WriteVerbose($"Writing to file '{File}' on VM {VmId} via guest agent...");
            var service = new VmService();
            service.WriteGuestFile(session, Node, VmId, File, Content);
        }
    }
}
