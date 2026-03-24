using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.HA;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.HA
{
    /// <summary>
    /// <para type="synopsis">Gets HA managed resources.</para>
    /// <para type="description">
    /// Lists all resources managed by the HA manager, or retrieves a specific
    /// resource by its service ID (e.g. "vm:100", "ct:200").
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveHaResource")]
    [OutputType(typeof(PveHaResource))]
    public sealed class GetPveHaResourceCmdlet : PveCmdletBase
    {
        /// <summary>Optional service ID to retrieve a specific resource.</summary>
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true,
            HelpMessage = "Service ID (e.g. 'vm:100', 'ct:200'). Omit to list all.")]
        public string? Sid { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new HaService();

            if (!string.IsNullOrEmpty(Sid))
            {
                WriteVerbose($"Getting HA resource '{Sid}'...");
                var resource = service.GetResource(session, Sid!);
                WriteObject(resource);
            }
            else
            {
                WriteVerbose("Listing all HA resources...");
                var resources = service.GetResources(session);
                foreach (var r in resources)
                    WriteObject(r);
            }
        }
    }
}
