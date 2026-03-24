using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Updates an HA group.</para>
    /// <para type="description">
    /// Modifies an existing HA group's node list, restriction settings, or comment.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveHaGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveHaGroupCmdlet : PveCmdletBase
    {
        /// <summary>Group name to update.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "HA group name.")]
        public string Group { get; set; } = string.Empty;

        /// <summary>Updated node list.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Node list: 'node1:pri,node2:pri'.")]
        public string? Nodes { get; set; }

        /// <summary>Restrict resources to this group's nodes.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Restrict resources to this group's nodes (0 or 1).")]
        [ValidateRange(0, 1)]
        public int? Restricted { get; set; }

        /// <summary>Disable failback.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Disable automatic failback (0 or 1).")]
        [ValidateRange(0, 1)]
        public int? NoFailback { get; set; }

        /// <summary>Description/comment.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Description or comment.")]
        public string? Comment { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"HA group '{Group}'", "Update"))
                return;

            var session = GetSession();
            var service = new HaService();

            var data = new Dictionary<string, string>();
            if (Nodes != null) data["nodes"] = Nodes;
            if (Restricted.HasValue) data["restricted"] = Restricted.Value.ToString();
            if (NoFailback.HasValue) data["nofailback"] = NoFailback.Value.ToString();
            if (Comment != null) data["comment"] = Comment;

            WriteVerbose($"Updating HA group '{Group}'...");
            service.UpdateGroup(session, Group, data);
        }
    }
}
