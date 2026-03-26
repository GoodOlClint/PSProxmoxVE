using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Adds a node to the cluster configuration.</para>
    /// <para type="description">
    /// Registers a new node in the Corosync cluster configuration.
    /// This is the server-side counterpart of joining — run this on an existing
    /// cluster member to prepare for a new node.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "PveClusterConfigNode", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class AddPveClusterConfigNodeCmdlet : PveCmdletBase
    {
        /// <summary>The name of the node to add.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The cluster node name to add.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>IP address of the new node (fallback if no links given).</summary>
        [Parameter(Mandatory = false, HelpMessage = "IP address of the new node.")]
        public string? NewNodeIp { get; set; }

        /// <summary>Node ID for the new node.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Corosync node ID.")]
        [ValidateRange(1, int.MaxValue)]
        public int? NodeId { get; set; }

        /// <summary>Number of votes for the new node.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Number of quorum votes.")]
        [ValidateRange(0, int.MaxValue)]
        public int? Votes { get; set; }

        /// <summary>Do not throw error if node already exists.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Force add even if node already exists.")]
        public SwitchParameter Force { get; set; }

        /// <summary>The JOIN_API_VERSION of the new node.</summary>
        [Parameter(Mandatory = false, HelpMessage = "JOIN_API_VERSION of the new node.")]
        public int? ApiVersion { get; set; }

        /// <summary>Corosync link addresses (link0..link7).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Corosync link addresses as key=value strings (e.g. 'link0=10.0.0.1').")]
        public string[]? Links { get; set; }

        /// <summary>Wait for the task to complete.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"node '{Node}'", "Add to cluster configuration"))
                return;

            var session = GetSession();
            var service = new ClusterConfigService();

            var linkDict = ParseLinks(Links);

            WriteVerbose($"Adding node '{Node}' to cluster configuration...");
            var upid = service.AddConfigNode(session, Node, NewNodeIp, linkDict, NodeId, Votes,
                Force.IsPresent ? true : (bool?)null, ApiVersion);

            var nodeName = GetNodeFromUpid(upid, session.Hostname);

            var task = new PveTask { Upid = upid, Status = "running", Node = nodeName };

            if (Wait.IsPresent && !string.IsNullOrEmpty(upid))
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, nodeName, upid);
            }

            WriteObject(task);
        }
    }
}
