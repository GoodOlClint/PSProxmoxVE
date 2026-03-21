using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Backup
{
    /// <summary>
    /// <para type="synopsis">Creates an ad-hoc backup of a virtual machine via vzdump.</para>
    /// <para type="description">
    /// Triggers an immediate backup (vzdump) of the specified VM on the given node.
    /// Returns a PveTask representing the backup operation. Use -Wait to block until
    /// the backup completes.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveBackup", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class NewPveBackupCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the VM resides.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the VM to back up.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The target storage for the backup.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The target storage for the backup.")]
        public string? Storage { get; set; }

        /// <summary>
        /// <para type="description">The backup mode (snapshot, suspend, or stop).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The backup mode.")]
        [ValidateSet("snapshot", "suspend", "stop")]
        public string? Mode { get; set; }

        /// <summary>
        /// <para type="description">The compression algorithm to use.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The compression algorithm.")]
        [ValidateSet("zstd", "lzo", "gzip", "none")]
        public string? Compress { get; set; }

        /// <summary>
        /// <para type="description">When specified, waits for the backup task to complete before returning.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Wait for the task to complete before returning.")]
        public SwitchParameter Wait { get; set; }

        /// <summary>
        /// <para type="description">Maximum seconds to wait when -Wait is specified. Default 3600.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Timeout in seconds for -Wait (default 3600).")]
        public int Timeout { get; set; } = 3600;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"VM {VmId} on node '{Node}'", "New-PveBackup"))
                return;

            var session = GetSession();
            var service = new BackupService();

            var formData = new Dictionary<string, string>
            {
                ["vmid"] = VmId.ToString()
            };

            if (!string.IsNullOrEmpty(Storage)) formData["storage"] = Storage!;
            if (!string.IsNullOrEmpty(Mode)) formData["mode"] = Mode!;
            if (!string.IsNullOrEmpty(Compress)) formData["compress"] = Compress!;

            WriteVerbose($"Creating backup of VM {VmId} on node '{Node}'...");
            var task = service.CreateBackup(session, Node, formData);

            if (Wait.IsPresent)
            {
                var taskService = new TaskService();
                task = taskService.WaitForTask(session, task.Node ?? Node, task.Upid, TimeSpan.FromSeconds(Timeout));
            }

            WriteObject(task);
        }
    }
}
