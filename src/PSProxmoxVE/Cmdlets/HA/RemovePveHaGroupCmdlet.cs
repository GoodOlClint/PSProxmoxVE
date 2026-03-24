using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Deletes an HA group.</para>
    /// <para type="description">
    /// Removes an HA group definition. Resources assigned to this group will
    /// need to be reassigned before deletion if the group is in use.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveHaGroup", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveHaGroupCmdlet : PveCmdletBase
    {
        /// <summary>Group name to delete.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "HA group name to delete.")]
        public string Group { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"HA group '{Group}'", "Delete"))
                return;

            var session = GetSession();
            var service = new HaService();

            WriteVerbose($"Deleting HA group '{Group}'...");
            service.DeleteGroup(session, Group);
        }
    }
}
