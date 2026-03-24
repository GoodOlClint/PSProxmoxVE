using System.Management.Automation;
using PSProxmoxVE.Core.Models.HA;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Gets HA groups.</para>
    /// <para type="description">
    /// Lists all HA groups or retrieves a specific group by name. HA groups
    /// define which nodes a resource can run on and their priorities.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveHaGroup")]
    [OutputType(typeof(PveHaGroup))]
    public sealed class GetPveHaGroupCmdlet : PveCmdletBase
    {
        /// <summary>Optional group name to retrieve.</summary>
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "HA group name. Omit to list all groups.")]
        public string? Group { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new HaService();

            if (!string.IsNullOrEmpty(Group))
            {
                WriteVerbose($"Getting HA group '{Group}'...");
                var group = service.GetGroup(session, Group!);
                WriteObject(group);
            }
            else
            {
                WriteVerbose("Listing all HA groups...");
                var groups = service.GetGroups(session);
                foreach (var g in groups)
                    WriteObject(g);
            }
        }
    }
}
