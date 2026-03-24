using System.Management.Automation;
using PSProxmoxVE.Core.Models.HA;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Gets the current HA manager status.</para>
    /// <para type="description">
    /// Returns the current status of the HA manager including quorum state,
    /// manager status, and service status entries.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveHaStatus")]
    [OutputType(typeof(PveHaStatus))]
    public sealed class GetPveHaStatusCmdlet : PveCmdletBase
    {
        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new HaService();

            WriteVerbose("Getting HA status...");
            var statuses = service.GetStatus(session);

            foreach (var status in statuses)
                WriteObject(status);
        }
    }
}
