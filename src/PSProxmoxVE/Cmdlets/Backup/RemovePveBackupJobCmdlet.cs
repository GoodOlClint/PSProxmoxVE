using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Backup
{
    /// <summary>
    /// <para type="synopsis">Removes a scheduled backup job from Proxmox VE.</para>
    /// <para type="description">
    /// Deletes a backup job from the Proxmox VE cluster configuration.
    /// This operation is destructive and requires confirmation unless -Force is specified.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveBackupJob",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveBackupJobCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The backup job ID to remove.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The backup job ID.")]
        public string Id { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"backup job '{Id}'", "Remove-PveBackupJob"))
                return;

            var session = GetSession();
            var service = new BackupService();

            WriteVerbose($"Removing backup job '{Id}'...");
            service.RemoveBackupJob(session, Id);
        }
    }
}
