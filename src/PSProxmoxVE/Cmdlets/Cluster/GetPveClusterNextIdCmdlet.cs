using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Gets the next available VM/CT ID in the cluster.</para>
    /// <para type="description">
    /// Returns the next free VMID from the cluster's auto-allocation pool.
    /// Optionally validates whether a specific VMID is available.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveClusterNextId")]
    [OutputType(typeof(int))]
    public sealed class GetPveClusterNextIdCmdlet : PveCmdletBase
    {
        /// <summary>Optional VMID to check availability for.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "Optional VMID to check availability for.")]
        [ValidateRange(100, 999999999)]
        public int? VmId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new ClusterConfigService();

            WriteVerbose("Getting next available cluster VMID...");
            var nextId = service.GetNextId(session, VmId);
            WriteObject(nextId);
        }
    }
}
