using System.Management.Automation;
using PSProxmoxVE.Core.Models.HA;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Gets HA rules.</para>
    /// <para type="description">
    /// Lists all HA rules or retrieves a specific rule by ID.
    /// Requires Proxmox VE 9.0 or later.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveHaRule")]
    [OutputType(typeof(PveHaRule))]
    public sealed class GetPveHaRuleCmdlet : PveCmdletBase
    {
        /// <summary>Optional rule ID.</summary>
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "HA rule ID. Omit to list all rules.")]
        public string? Rule { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "HA Rules", 9, 0);

            var service = new HaService();

            if (!string.IsNullOrEmpty(Rule))
            {
                WriteVerbose($"Getting HA rule '{Rule}'...");
                var rule = service.GetRule(session, Rule!);
                WriteObject(rule);
            }
            else
            {
                WriteVerbose("Listing all HA rules...");
                var rules = service.GetRules(session);
                foreach (var r in rules)
                    WriteObject(r);
            }
        }
    }
}
