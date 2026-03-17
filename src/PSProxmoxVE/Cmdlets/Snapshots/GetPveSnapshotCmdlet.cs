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
    public class GetPveSnapshotCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// The VM identifier. Accepts pipeline input from Get-PveVm (PveVm.VmId).
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>Optional filter: return only the snapshot with this name.</summary>
        [Parameter(Mandatory = false)]
        public string? Name { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
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
