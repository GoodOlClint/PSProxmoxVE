using System.Management.Automation;
using PSProxmoxVE.Core.Models.Cluster;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Gets the cluster configuration.</para>
    /// <para type="description">
    /// Returns the cluster configuration directory (GET /cluster/config): one entry
    /// per available sub-resource (nodes, totem, qdevice, join, apiversion).
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveClusterConfig")]
    [OutputType(typeof(PveClusterConfigEntry))]
    public sealed class GetPveClusterConfigCmdlet : PveCmdletBase
    {
        protected override void ProcessPveRecord()
        {
            var session = GetSession();
            var service = new ClusterConfigService();

            WriteVerbose("Getting cluster configuration...");
            var entries = service.GetClusterConfig(session);
            foreach (var entry in entries)
            {
                WriteObject(entry);
            }
        }
    }
}
