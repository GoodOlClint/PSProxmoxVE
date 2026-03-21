using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Returns the configuration of a Proxmox VE node.</para>
    /// <para type="description">
    /// Retrieves the node configuration from the /nodes/{node}/config endpoint,
    /// including description, wakeonlan settings and other node-level properties.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveNodeConfig")]
    [OutputType(typeof(PSObject))]
    public class GetPveNodeConfigCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new NodeService();

            WriteVerbose($"Getting configuration for node '{Node}'...");
            var config = service.GetNodeConfig(session, Node);

            var psObj = new PSObject();
            foreach (var prop in config.Properties())
            {
                psObj.Properties.Add(new PSNoteProperty(prop.Name, prop.Value.ToString()));
            }

            WriteObject(psObj);
        }
    }
}
