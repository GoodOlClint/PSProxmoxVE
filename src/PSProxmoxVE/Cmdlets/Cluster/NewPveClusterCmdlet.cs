using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Creates a new Proxmox VE cluster.</para>
    /// <para type="description">
    /// Initializes a new Corosync cluster on the current node. This is a
    /// destructive operation that should only be performed once per cluster.
    /// Additional nodes can then join using Add-PveClusterMember.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveCluster", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(string))]
    public sealed class NewPveClusterCmdlet : PveCmdletBase
    {
        /// <summary>The name for the new cluster.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The name for the new cluster (max 15 chars).")]
        [ValidateLength(1, 15)]
        public string ClusterName { get; set; } = string.Empty;

        /// <summary>Node ID for this node.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Corosync node ID for this node.")]
        [ValidateRange(1, int.MaxValue)]
        public int? NodeId { get; set; }

        /// <summary>Number of quorum votes for this node.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Number of quorum votes.")]
        [ValidateRange(1, int.MaxValue)]
        public int? Votes { get; set; }

        /// <summary>Corosync link addresses (link0..link7).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Corosync link addresses as key=value strings (e.g. 'link0=10.0.0.1').")]
        public string[]? Links { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"cluster '{ClusterName}'", "Create new cluster"))
                return;

            var session = GetSession();
            var service = new ClusterConfigService();

            var linkDict = ParseLinks(Links);

            WriteVerbose($"Creating cluster '{ClusterName}'...");
            var upid = service.CreateCluster(session, ClusterName, linkDict, NodeId, Votes);
            WriteObject(upid);
        }
    }
}
