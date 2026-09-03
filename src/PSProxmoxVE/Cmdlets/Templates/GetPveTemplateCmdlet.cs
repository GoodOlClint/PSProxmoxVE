using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Templates
{
    /// <summary>
    /// <para type="synopsis">Lists VM templates on a Proxmox VE node.</para>
    /// <para type="description">
    /// Returns QEMU virtual machines that are marked as templates on the specified node.
    /// Optionally filter by template name pattern.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveTemplate")]
    [OutputType(typeof(PveVm))]
    public sealed class GetPveTemplateCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name. When omitted, queries all nodes in the cluster.</summary>
        [Parameter(Mandatory = false, Position = 0, HelpMessage = "The PVE node name.")]
        public string? Node { get; set; }

        /// <summary>Filter results by template name. Supports wildcard (*) matching.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Filter by template name (supports wildcards).")]
        public string? Name { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new TemplateService();

            WriteVerbose("Getting templates...");
            var queryNode = string.IsNullOrEmpty(Node) ? null : Node;
            var templates = service.GetTemplates(session, queryNode,
                onNodeSkipped: (nodeName, ex) => WriteWarning($"Skipping node '{nodeName}': {ex.Message}"));

            foreach (var vm in templates)
            {
                if (vm.Node == null && queryNode != null) vm.Node = queryNode;

                if (!string.IsNullOrEmpty(Name) && vm.Name != null)
                {
                    var pattern = Name!.Replace("*", "");
                    if (Name.Contains("*"))
                    {
                        if (vm.Name.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                    }
                    else
                    {
                        if (!string.Equals(vm.Name, Name, System.StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                }

                WriteObject(vm);
            }
        }
    }
}
