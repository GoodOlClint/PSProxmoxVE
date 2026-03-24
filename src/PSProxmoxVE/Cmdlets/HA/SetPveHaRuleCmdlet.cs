using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Updates an HA rule.</para>
    /// <para type="description">
    /// Modifies an existing HA rule. Requires Proxmox VE 9.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveHaRule", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveHaRuleCmdlet : PveCmdletBase
    {
        /// <summary>Rule ID to update.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "HA rule ID.")]
        public string Rule { get; set; } = string.Empty;

        /// <summary>Rule state.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Rule state: enabled or disabled.")]
        [ValidateSet("enabled", "disabled")]
        public string? State { get; set; }

        /// <summary>Description/comment.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Description or comment.")]
        public string? Comment { get; set; }

        /// <summary>Additional rule properties.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Additional rule properties.")]
        public System.Collections.Hashtable? Properties { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "HA Rules", 9, 0);

            if (!ShouldProcess($"HA rule '{Rule}'", "Update"))
                return;

            var service = new HaService();

            var data = new Dictionary<string, string>();
            if (State != null) data["state"] = State;
            if (Comment != null) data["comment"] = Comment;
            if (Properties != null)
            {
                foreach (var key in Properties.Keys)
                    data[key.ToString()!] = Properties[key]!.ToString()!;
            }

            WriteVerbose($"Updating HA rule '{Rule}'...");
            service.UpdateRule(session, Rule, data);
        }
    }
}
