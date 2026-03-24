using System.Management.Automation;
using PSProxmoxVE.Core.Models.Cluster;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Gets cluster-wide datacenter options.</para>
    /// <para type="description">
    /// Returns the datacenter-level cluster options including keyboard layout,
    /// language, migration settings, HA settings, and more.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveClusterOption")]
    [OutputType(typeof(PveClusterOptions))]
    public sealed class GetPveClusterOptionCmdlet : PveCmdletBase
    {
        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new ClusterConfigService();

            WriteVerbose("Getting cluster options...");
            var options = service.GetClusterOptions(session);
            WriteObject(options);
        }
    }
}
