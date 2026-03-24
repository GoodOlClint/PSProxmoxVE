using System.Management.Automation;
using PSProxmoxVE.Core.Models.Cluster;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Gets cluster join information.</para>
    /// <para type="description">
    /// Returns the information needed to join this cluster, including the
    /// certificate fingerprint, node list, and totem configuration.
    /// Run this on an existing cluster member to get join credentials.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveClusterJoinInfo")]
    [OutputType(typeof(PveClusterJoinInfo))]
    public sealed class GetPveClusterJoinInfoCmdlet : PveCmdletBase
    {
        /// <summary>Node to get join info for (defaults to current node).</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "Node to get join info for (defaults to current).")]
        public string? Node { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new ClusterConfigService();

            WriteVerbose("Getting cluster join information...");
            var joinInfo = service.GetJoinInfo(session, Node);
            WriteObject(joinInfo);
        }
    }
}
