using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Creates a new HA rule.</para>
    /// <para type="description">
    /// Creates a new HA rule for the cluster. Requires Proxmox VE 9.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveHaRule", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class NewPveHaRuleCmdlet : PveCmdletBase
    {
        /// <summary>Rule type.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "Rule type.")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Rule state.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Rule state: enabled or disabled.")]
        [ValidateSet("enabled", "disabled")]
        public string? State { get; set; }

        /// <summary>Description/comment.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Description or comment.")]
        public string? Comment { get; set; }

        /// <summary>Additional rule properties as a hashtable.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Additional rule properties.")]
        public System.Collections.Hashtable? Properties { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "HA Rules", 9, 0);

            if (!ShouldProcess($"HA rule of type '{Type}'", "Create"))
                return;

            var service = new HaService();

            var data = new Dictionary<string, string> { ["type"] = Type };
            if (State != null) data["state"] = State;
            if (Comment != null) data["comment"] = Comment;
            if (Properties != null)
            {
                foreach (var key in Properties.Keys)
                {
                    var value = Properties[key]?.ToString();
                    if (key != null && value != null)
                        data[key.ToString()!] = value;
                }
            }

            WriteVerbose($"Creating HA rule of type '{Type}'...");
            service.CreateRule(session, data);
        }
    }
}
