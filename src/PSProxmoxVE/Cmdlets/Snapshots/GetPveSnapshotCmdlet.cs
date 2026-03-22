using System.Linq;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Snapshots
{
    /// <summary>
    /// <para type="synopsis">Lists snapshots for a Proxmox VE virtual machine.</para>
    /// <para type="description">
    /// Returns all snapshots for the specified VM on the given node.
    /// VmId can be piped from Get-PveVm.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveSnapshot")]
    [OutputType(typeof(PveSnapshot))]
    public sealed class GetPveSnapshotCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// The VM identifier. Accepts pipeline input from Get-PveVm (PveVm.VmId).
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>Optional filter: return only the snapshot with this name.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by snapshot name.")]
        public string? Name { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose($"Getting snapshots for VM {VmId} on node '{Node}'...");
            var service = new SnapshotService();

            var snapshots = service.GetSnapshots(session, Node, VmId);

            if (!string.IsNullOrEmpty(Name))
                snapshots = snapshots.Where(s => s.Name == Name).ToArray();

            foreach (var snapshot in snapshots)
            {
                snapshot.VmId = VmId;
                snapshot.Node = Node;
                WriteObject(snapshot);
            }
        }
    }
}
