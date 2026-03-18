using System.Management.Automation;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Vms;

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
    public class GetPveTemplateCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name. When omitted, queries all nodes in the cluster.</summary>
        [Parameter(Mandatory = false, Position = 0)]
        public string? Node { get; set; }

        /// <summary>Filter results by template name. Supports wildcard (*) matching.</summary>
        [Parameter(Mandatory = false)]
        public string? Name { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            using var client = new PveHttpClient(session);

            var nodesToQuery = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(Node))
            {
                nodesToQuery.Add(Node);
            }
            else
            {
                var nodesJson = client.GetAsync("nodes").GetAwaiter().GetResult();
                var nodesRoot = JObject.Parse(nodesJson);
                var nodesData = nodesRoot["data"] as JArray ?? new JArray();
                foreach (var n in nodesData)
                    nodesToQuery.Add(n["node"]?.ToString() ?? string.Empty);
            }

            foreach (var node in nodesToQuery)
            {
                if (string.IsNullOrEmpty(node)) continue;

                var json = client.GetAsync($"nodes/{node}/qemu").GetAwaiter().GetResult();
                var root = JObject.Parse(json);
                var data = root["data"] as JArray ?? new JArray();

                foreach (var item in data)
                {
                    var vm = item.ToObject<PveVm>()!;
                    if (vm.Node == null) vm.Node = node;

                    // Only return templates
                    if (vm.Template != 1) continue;

                    if (!string.IsNullOrEmpty(Name) && vm.Name != null)
                    {
                        var pattern = Name.Replace("*", "");
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
}
