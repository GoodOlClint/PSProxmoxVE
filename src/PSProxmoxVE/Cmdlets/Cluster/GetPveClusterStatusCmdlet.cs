using System.Management.Automation;
using PSProxmoxVE.Core.Models.Cluster;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Gets the current cluster status.</para>
    /// <para type="description">
    /// Returns the cluster status including cluster-wide info and per-node entries.
    /// Filter by Type to distinguish cluster vs node records.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveClusterStatus")]
    [OutputType(typeof(PveClusterStatus))]
    public sealed class GetPveClusterStatusCmdlet : PveCmdletBase
    {
        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new ClusterConfigService();

            WriteVerbose("Getting cluster status...");
            var statuses = service.GetClusterStatus(session);

            foreach (var status in statuses)
                WriteObject(status);
        }
    }
}
