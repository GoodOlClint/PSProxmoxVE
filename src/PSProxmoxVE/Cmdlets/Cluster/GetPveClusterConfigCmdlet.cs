using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Gets the cluster configuration.</para>
    /// <para type="description">
    /// Returns the raw cluster configuration including nodes, totem settings,
    /// and cluster version information.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveClusterConfig")]
    [OutputType(typeof(PSObject))]
    public sealed class GetPveClusterConfigCmdlet : PveCmdletBase
    {
        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new ClusterConfigService();

            WriteVerbose("Getting cluster configuration...");
            var config = service.GetClusterConfig(session);
            WriteObject(config);
        }
    }
}
