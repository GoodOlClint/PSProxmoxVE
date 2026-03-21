using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Vms
{
    /// <summary>
    /// <para type="synopsis">Reads a file from the guest filesystem via the QEMU guest agent.</para>
    /// <para type="description">
    /// Reads the contents of a file inside the guest operating system using the QEMU guest agent.
    /// The guest agent must be installed and running inside the VM.
    /// Returns the file content as a string.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Read, "PveVmGuestFile")]
    [OutputType(typeof(string))]
    public sealed class ReadPveVmGuestFileCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The path to the file inside the guest to read.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The file path inside the guest to read.")]
        public string File { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose($"Reading file '{File}' from VM {VmId} via guest agent...");
            var service = new VmService();
            var content = service.ReadGuestFile(session, Node, VmId, File);

            WriteObject(content);
        }
    }
}
