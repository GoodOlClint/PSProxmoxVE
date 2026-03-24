using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Requests HA migration or relocation of a resource.</para>
    /// <para type="description">
    /// Requests the HA manager to migrate or relocate a managed resource to a
    /// different node. Use -Mode Migrate for online migration or -Mode Relocate
    /// for offline relocation.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "PveHaResource", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class MovePveHaResourceCmdlet : PveCmdletBase
    {
        /// <summary>Service ID of the resource to move.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "Service ID (e.g. 'vm:100', 'ct:200').")]
        public string Sid { get; set; } = string.Empty;

        /// <summary>Target node name.</summary>
        [Parameter(Mandatory = true, Position = 1, HelpMessage = "Target node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>Migration mode: Migrate (online) or Relocate (offline).</summary>
        [Parameter(Mandatory = false, HelpMessage = "Migration mode: Migrate (online) or Relocate (offline).")]
        [ValidateSet("Migrate", "Relocate")]
        public string Mode { get; set; } = "Migrate";

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"HA resource '{Sid}' to node '{Node}'", $"{Mode}"))
                return;

            var session = GetSession();
            var service = new HaService();

            WriteVerbose($"{Mode} HA resource '{Sid}' to node '{Node}'...");
            if (string.Equals(Mode, "Relocate", System.StringComparison.OrdinalIgnoreCase))
                service.RelocateResource(session, Sid, Node);
            else
                service.MigrateResource(session, Sid, Node);
        }
    }
}
