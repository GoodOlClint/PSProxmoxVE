using System.Management.Automation;
using PSProxmoxVE.Core.Models.Cluster;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Lists nodes in the cluster configuration.</para>
    /// <para type="description">
    /// Returns all nodes registered in the Corosync cluster configuration,
    /// including their node IDs and ring addresses.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveClusterConfigNode")]
    [OutputType(typeof(PveClusterConfigNode))]
    public sealed class GetPveClusterConfigNodeCmdlet : PveCmdletBase
    {
        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new ClusterConfigService();

            WriteVerbose("Getting cluster config nodes...");
            var nodes = service.GetConfigNodes(session);

            foreach (var node in nodes)
                WriteObject(node);
        }
    }
}
