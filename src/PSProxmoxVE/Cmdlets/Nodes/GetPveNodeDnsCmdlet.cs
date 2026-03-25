using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Returns the DNS configuration of a Proxmox VE node.</para>
    /// <para type="description">
    /// Retrieves the DNS settings (search domain and nameservers) for the specified
    /// node from the /nodes/{node}/dns endpoint.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveNodeDns")]
    [OutputType(typeof(PSObject))]
    public sealed class GetPveNodeDnsCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new NodeService();

            WriteVerbose($"Getting DNS configuration for node '{Node}'...");
            var dns = service.GetNodeDns(session, Node);

            var psObj = new PSObject();
            foreach (var kvp in dns)
            {
                psObj.Properties.Add(new PSNoteProperty(kvp.Key, kvp.Value));
            }

            WriteObject(psObj);
        }
    }
}
