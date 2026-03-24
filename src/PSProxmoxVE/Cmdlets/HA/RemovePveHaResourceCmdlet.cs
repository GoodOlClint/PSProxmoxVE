using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Removes a resource from HA management.</para>
    /// <para type="description">
    /// Removes a VM or container from HA management. The resource will no longer
    /// be monitored for failover.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveHaResource", SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveHaResourceCmdlet : PveCmdletBase
    {
        /// <summary>Service ID of the resource to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "Service ID (e.g. 'vm:100', 'ct:200').")]
        public string Sid { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"HA resource '{Sid}'", "Remove from HA management"))
                return;

            var session = GetSession();
            var service = new HaService();

            WriteVerbose($"Removing HA resource '{Sid}'...");
            service.DeleteResource(session, Sid);
        }
    }
}
