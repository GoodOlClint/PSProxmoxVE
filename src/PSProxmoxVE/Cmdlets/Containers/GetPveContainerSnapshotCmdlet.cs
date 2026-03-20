using System.Linq;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Lists snapshots for a Proxmox VE container.</para>
    /// <para type="description">
    /// Returns all snapshots for the specified LXC container on the given node.
    /// VmId can be piped from Get-PveContainer.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveContainerSnapshot")]
    [OutputType(typeof(PveSnapshot))]
    public class GetPveContainerSnapshotCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// The container identifier. Accepts pipeline input from Get-PveContainer (PveContainer.VmId).
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>Optional filter: return only the snapshot with this name.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by snapshot name.")]
        public string? Name { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            WriteVerbose($"Getting snapshots for container {VmId} on node '{Node}'...");
            var service = new ContainerService();

            var snapshots = service.GetContainerSnapshots(session, Node, VmId);

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
