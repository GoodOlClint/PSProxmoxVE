using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Updates an HA managed resource.</para>
    /// <para type="description">
    /// Modifies the configuration of an existing HA managed resource,
    /// such as changing its state, group, or restart limits.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveHaResource", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveHaResourceCmdlet : PveCmdletBase
    {
        /// <summary>Service ID of the resource to update.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "Service ID (e.g. 'vm:100', 'ct:200').")]
        public string Sid { get; set; } = string.Empty;

        /// <summary>Desired state.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Desired state: started, stopped, disabled, ignored.")]
        [ValidateSet("started", "stopped", "disabled", "ignored")]
        public string? State { get; set; }

        /// <summary>HA group name.</summary>
        [Parameter(Mandatory = false, HelpMessage = "HA group name.")]
        public string? Group { get; set; }

        /// <summary>Maximum relocations.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Maximum relocations before giving up.")]
        [ValidateRange(0, 10)]
        public int? MaxRelocate { get; set; }

        /// <summary>Maximum restart attempts.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Maximum restart attempts.")]
        [ValidateRange(0, 10)]
        public int? MaxRestart { get; set; }

        /// <summary>Description/comment.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Description or comment.")]
        public string? Comment { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"HA resource '{Sid}'", "Update"))
                return;

            var session = GetSession();
            var service = new HaService();

            var data = new Dictionary<string, string>();
            if (State != null) data["state"] = State;
            if (Group != null) data["group"] = Group;
            if (MaxRelocate.HasValue) data["max_relocate"] = MaxRelocate.Value.ToString();
            if (MaxRestart.HasValue) data["max_restart"] = MaxRestart.Value.ToString();
            if (Comment != null) data["comment"] = Comment;

            WriteVerbose($"Updating HA resource '{Sid}'...");
            service.UpdateResource(session, Sid, data);
        }
    }
}
