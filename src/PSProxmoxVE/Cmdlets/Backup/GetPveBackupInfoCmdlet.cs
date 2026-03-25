using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Backup
{
    /// <summary>
    /// <para type="synopsis">Lists guests not covered by any backup job.</para>
    /// <para type="description">
    /// Returns information about VMs and containers that are not included in any
    /// scheduled backup job. Useful for backup compliance auditing.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveBackupInfo")]
    [OutputType(typeof(PSObject))]
    public sealed class GetPveBackupInfoCmdlet : PveCmdletBase
    {
        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "Backup compliance info", 7, 0);
            var service = new BackupService();

            WriteVerbose("Getting guests not covered by backup jobs...");
            var items = service.GetNotBackedUp(session);

            foreach (var item in items)
            {
                var pso = new PSObject();
                foreach (var kvp in item)
                {
                    pso.Properties.Add(new PSNoteProperty(kvp.Key, kvp.Value));
                }
                WriteObject(pso);
            }
        }
    }
}
