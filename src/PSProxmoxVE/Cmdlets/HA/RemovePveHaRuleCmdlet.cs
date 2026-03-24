using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Deletes an HA rule.</para>
    /// <para type="description">
    /// Removes an HA rule from the cluster. Requires Proxmox VE 9.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveHaRule", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveHaRuleCmdlet : PveCmdletBase
    {
        /// <summary>Rule ID to delete.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "HA rule ID to delete.")]
        public string Rule { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "HA Rules", 9, 0);

            if (!ShouldProcess($"HA rule '{Rule}'", "Delete"))
                return;

            var service = new HaService();

            WriteVerbose($"Deleting HA rule '{Rule}'...");
            service.DeleteRule(session, Rule);
        }
    }
}
