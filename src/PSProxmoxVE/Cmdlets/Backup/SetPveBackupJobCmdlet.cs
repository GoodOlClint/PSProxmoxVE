using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Backup
{
    /// <summary>
    /// <para type="synopsis">Updates a scheduled backup job on Proxmox VE.</para>
    /// <para type="description">
    /// Modifies properties of an existing backup job. Only specified parameters are
    /// updated; omitted parameters retain their current values.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveBackupJob", SupportsShouldProcess = true)]
    public sealed class SetPveBackupJobCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The backup job ID to update.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The backup job ID.")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">Updated cron-style schedule expression.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Cron-style schedule expression (e.g., '0 2 * * *').")]
        public string? Schedule { get; set; }

        /// <summary>
        /// <para type="description">Updated target storage for backups.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The target storage for backups.")]
        public string? Storage { get; set; }

        /// <summary>
        /// <para type="description">Updated backup mode (snapshot, suspend, or stop).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The backup mode.")]
        [ValidateSet("snapshot", "suspend", "stop")]
        public string? Mode { get; set; }

        /// <summary>
        /// <para type="description">Updated comma-separated list of VM IDs.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of VM IDs to back up.")]
        public string? VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, backs up all VMs.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Back up all VMs.")]
        public SwitchParameter All { get; set; }

        /// <summary>
        /// <para type="description">Updated compression algorithm.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The compression algorithm.")]
        [ValidateSet("zstd", "lzo", "gzip", "none")]
        public string? Compress { get; set; }

        /// <summary>
        /// <para type="description">Updated node restriction.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The node to restrict the backup job to.")]
        public string? Node { get; set; }

        /// <summary>
        /// <para type="description">Updated email address(es) for notifications.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Email address(es) for notifications.")]
        public string? MailTo { get; set; }

        /// <summary>
        /// <para type="description">Updated notification policy.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "When to send email notifications.")]
        [ValidateSet("always", "failure")]
        public string? MailNotification { get; set; }

        /// <summary>
        /// <para type="description">Enable or disable the backup job.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Enable or disable the backup job.")]
        public SwitchParameter Enabled { get; set; }

        /// <summary>
        /// <para type="description">Updated comment or description.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "A comment or description for the backup job.")]
        public string? Comment { get; set; }

        /// <summary>
        /// <para type="description">Updated prune-backups configuration string.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Prune-backups configuration string.")]
        public string? PruneBackups { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"backup job '{Id}'", "Set-PveBackupJob"))
                return;

            var session = GetSession();
            var service = new BackupService();

            var config = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(Schedule)) config["schedule"] = Schedule!;
            if (!string.IsNullOrEmpty(Storage)) config["storage"] = Storage!;
            if (!string.IsNullOrEmpty(Mode)) config["mode"] = Mode!;
            if (!string.IsNullOrEmpty(VmId)) config["vmid"] = VmId!;
            if (All.IsPresent) config["all"] = "1";
            if (!string.IsNullOrEmpty(Compress)) config["compress"] = Compress!;
            if (!string.IsNullOrEmpty(Node)) config["node"] = Node!;
            if (!string.IsNullOrEmpty(MailTo)) config["mailto"] = MailTo!;
            if (!string.IsNullOrEmpty(MailNotification)) config["mailnotification"] = MailNotification!;
            if (MyInvocation.BoundParameters.ContainsKey("Enabled")) config["enabled"] = Enabled.IsPresent ? "1" : "0";
            if (!string.IsNullOrEmpty(Comment)) config["comment"] = Comment!;
            if (!string.IsNullOrEmpty(PruneBackups)) config["prune-backups"] = PruneBackups!;

            WriteVerbose($"Updating backup job '{Id}'...");
            service.UpdateBackupJob(session, Id, config);
        }
    }
}
