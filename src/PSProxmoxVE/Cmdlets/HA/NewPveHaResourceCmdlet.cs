using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Creates a new HA managed resource.</para>
    /// <para type="description">
    /// Adds a VM or container to HA management. The resource will be monitored
    /// and automatically restarted or migrated on node failure.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveHaResource", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class NewPveHaResourceCmdlet : PveCmdletBase
    {
        /// <summary>Service ID (e.g. "vm:100" or "ct:200").</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "Service ID (e.g. 'vm:100', 'ct:200').")]
        public string Sid { get; set; } = string.Empty;

        /// <summary>Desired state for the resource.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Desired state: started, stopped, disabled, ignored.")]
        [ValidateSet("started", "stopped", "disabled", "ignored")]
        public string? State { get; set; }

        /// <summary>HA group to assign this resource to.</summary>
        [Parameter(Mandatory = false, HelpMessage = "HA group name.")]
        public string? Group { get; set; }

        /// <summary>Maximum number of relocations before giving up.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Maximum relocations before giving up.")]
        [ValidateRange(0, 10)]
        public int? MaxRelocate { get; set; }

        /// <summary>Maximum number of restart attempts.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Maximum restart attempts.")]
        [ValidateRange(0, 10)]
        public int? MaxRestart { get; set; }

        /// <summary>Description/comment.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Description or comment.")]
        public string? Comment { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"HA resource '{Sid}'", "Create"))
                return;

            var session = GetSession();
            var service = new HaService();

            var options = new Dictionary<string, string>();
            if (State != null) options["state"] = State;
            if (Group != null) options["group"] = Group;
            if (MaxRelocate.HasValue) options["max_relocate"] = MaxRelocate.Value.ToString();
            if (MaxRestart.HasValue) options["max_restart"] = MaxRestart.Value.ToString();
            if (Comment != null) options["comment"] = Comment;

            WriteVerbose($"Creating HA resource '{Sid}'...");
            service.CreateResource(session, Sid, options);
        }
    }
}
