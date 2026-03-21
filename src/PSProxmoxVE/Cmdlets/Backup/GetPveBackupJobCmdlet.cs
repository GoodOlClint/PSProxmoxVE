using System.Management.Automation;
using PSProxmoxVE.Core.Models.Backup;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Backup
{
    /// <summary>
    /// <para type="synopsis">Lists Proxmox VE backup jobs.</para>
    /// <para type="description">
    /// Returns scheduled backup job configurations from the Proxmox VE cluster.
    /// Optionally filter by job ID.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveBackupJob")]
    [OutputType(typeof(PveBackupJob))]
    public sealed class GetPveBackupJobCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The backup job ID to retrieve. When omitted, all jobs are returned.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The backup job ID.")]
        public string? Id { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new BackupService();

            if (!string.IsNullOrEmpty(Id))
            {
                WriteVerbose($"Getting backup job '{Id}'...");
                var job = service.GetBackupJob(session, Id!);
                WriteObject(job);
            }
            else
            {
                WriteVerbose("Getting all backup jobs...");
                var jobs = service.GetBackupJobs(session);
                foreach (var job in jobs)
                {
                    WriteObject(job);
                }
            }
        }
    }
}
