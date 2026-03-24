using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Removes a node from the cluster configuration.</para>
    /// <para type="description">
    /// Removes a node from the Corosync cluster configuration. The node must be
    /// offline or already evacuated before removal. This is a destructive operation
    /// that changes cluster topology.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveClusterConfigNode", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveClusterConfigNodeCmdlet : PveCmdletBase
    {
        /// <summary>The name of the node to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The cluster node name to remove.")]
        public string Node { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"node '{Node}'", "Remove from cluster configuration"))
                return;

            var session = GetSession();
            var service = new ClusterConfigService();

            WriteVerbose($"Removing node '{Node}' from cluster configuration...");
            service.RemoveConfigNode(session, Node);
        }
    }
}
