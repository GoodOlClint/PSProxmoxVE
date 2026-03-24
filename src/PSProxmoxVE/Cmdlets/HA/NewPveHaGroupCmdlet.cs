using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Creates a new HA group.</para>
    /// <para type="description">
    /// Creates an HA group that defines which nodes a managed resource can run on
    /// and their priorities. Format for Nodes: "node1:priority,node2:priority".
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveHaGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class NewPveHaGroupCmdlet : PveCmdletBase
    {
        /// <summary>Group name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "HA group name.")]
        public string Group { get; set; } = string.Empty;

        /// <summary>Node list with priorities.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "Node list: 'node1:pri,node2:pri'.")]
        public string Nodes { get; set; } = string.Empty;

        /// <summary>Restrict resources to this group's nodes.</summary>
        [Parameter(Mandatory = false, HelpMessage = "If set, resources can only run on this group's nodes.")]
        public SwitchParameter Restricted { get; set; }

        /// <summary>Disable failback to higher-priority nodes.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Disable automatic failback to higher-priority nodes.")]
        public SwitchParameter NoFailback { get; set; }

        /// <summary>Description/comment.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Description or comment.")]
        public string? Comment { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"HA group '{Group}'", "Create"))
                return;

            var session = GetSession();
            var service = new HaService();

            var options = new Dictionary<string, string>();
            if (Restricted.IsPresent) options["restricted"] = "1";
            if (NoFailback.IsPresent) options["nofailback"] = "1";
            if (Comment != null) options["comment"] = Comment;

            WriteVerbose($"Creating HA group '{Group}'...");
            service.CreateGroup(session, Group, Nodes, options);
        }
    }
}
