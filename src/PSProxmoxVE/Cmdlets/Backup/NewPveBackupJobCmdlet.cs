using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Backup
{
    /// <summary>
    /// <para type="synopsis">Creates a scheduled backup job on Proxmox VE.</para>
    /// <para type="description">
    /// Creates a new scheduled backup job in the Proxmox VE cluster configuration.
    /// Specify a cron-style schedule and target storage. Use VmId for specific VMs
    /// or -All to back up all VMs.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveBackupJob", SupportsShouldProcess = true)]
    public sealed class NewPveBackupJobCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">Cron-style schedule expression (e.g., "0 2 * * *").</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Cron-style schedule expression (e.g., '0 2 * * *').")]
        public string Schedule { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The target storage for backups.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The target storage for backups.")]
        public string Storage { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The backup mode (snapshot, suspend, or stop).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The backup mode.")]
        [ValidateSet("snapshot", "suspend", "stop")]
        public string? Mode { get; set; }

        /// <summary>
        /// <para type="description">Comma-separated list of VM IDs to include in the backup job.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of VM IDs to back up.")]
        public string? VmId { get; set; }

        /// <summary>
        /// <para type="description">When specified, backs up all VMs on the node.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Back up all VMs.")]
        public SwitchParameter All { get; set; }

        /// <summary>
        /// <para type="description">The compression algorithm to use.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The compression algorithm.")]
        [ValidateSet("zstd", "lzo", "gzip", "none")]
        public string? Compress { get; set; }

        /// <summary>
        /// <para type="description">The node to restrict the backup job to.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The node to restrict the backup job to.")]
        public string? Node { get; set; }

        /// <summary>
        /// <para type="description">Email address(es) to send notifications to.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Email address(es) for notifications.")]
        public string? MailTo { get; set; }

        /// <summary>
        /// <para type="description">When to send email notifications (always or failure).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "When to send email notifications.")]
        [ValidateSet("always", "failure")]
        public string? MailNotification { get; set; }

        /// <summary>
        /// <para type="description">Whether the backup job is enabled. Enabled by default unless -Enabled:$false is specified.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Whether the backup job is enabled. Default is enabled.")]
        public SwitchParameter Enabled { get; set; }

        /// <summary>
        /// <para type="description">A comment or description for the backup job.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "A comment or description for the backup job.")]
        public string? Comment { get; set; }

        /// <summary>
        /// <para type="description">Prune-backups configuration string (e.g., "keep-daily=7,keep-weekly=4").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Prune-backups configuration string.")]
        public string? PruneBackups { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess("cluster backup configuration", "New-PveBackupJob"))
                return;

            var session = GetSession();
            var service = new BackupService();

            var config = new Dictionary<string, string>
            {
                ["schedule"] = Schedule,
                ["storage"] = Storage,
                ["enabled"] = MyInvocation.BoundParameters.ContainsKey("Enabled") && !Enabled.IsPresent ? "0" : "1"
            };

            if (!string.IsNullOrEmpty(Mode)) config["mode"] = Mode!;
            if (!string.IsNullOrEmpty(VmId)) config["vmid"] = VmId!;
            if (All.IsPresent) config["all"] = "1";
            if (!string.IsNullOrEmpty(Compress)) config["compress"] = Compress!;
            if (!string.IsNullOrEmpty(Node)) config["node"] = Node!;
            if (!string.IsNullOrEmpty(MailTo)) config["mailto"] = MailTo!;
            if (!string.IsNullOrEmpty(MailNotification)) config["mailnotification"] = MailNotification!;
            if (!string.IsNullOrEmpty(Comment)) config["comment"] = Comment!;
            if (!string.IsNullOrEmpty(PruneBackups)) config["prune-backups"] = PruneBackups!;

            WriteVerbose("Creating backup job...");
            service.CreateBackupJob(session, config);
        }
    }
}
