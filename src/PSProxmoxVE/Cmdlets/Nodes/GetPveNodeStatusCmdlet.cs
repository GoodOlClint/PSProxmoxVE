using System;
using System.Management.Automation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Nodes;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Returns detailed status information for a Proxmox VE node.</para>
    /// <para type="description">
    /// Get-PveNodeStatus retrieves CPU, memory, disk, swap, network and uptime statistics
    /// for the specified node from the /nodes/{node}/status endpoint.
    /// The -Node parameter accepts values from the pipeline by property name, so you can
    /// pipe output from Get-PveNode directly.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveNodeStatus")]
    [OutputType(typeof(PveNodeStatus))]
    public sealed class GetPveNodeStatusCmdlet : PveCmdletBase
    {
        /// <summary>
        /// Name of the node to query. Accepts pipeline input via the PveNode.Name property.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Node { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            var session = GetSession();

            var resource = $"/api2/json/nodes/{Uri.EscapeDataString(Node)}/status";

            string responseBody;
            try
            {
                using var client = new PveHttpClient(session);
                responseBody = client.GetAsync(resource).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(
                    ex,
                    "GetPveNodeStatusFailed",
                    ErrorCategory.ConnectionError,
                    Node));
                return;
            }

            var json = JObject.Parse(responseBody);
            var dataToken = json["data"] ?? throw new InvalidOperationException("Response did not contain a 'data' field.");

            var status = dataToken.ToObject<PveNodeStatus>(JsonSerializer.CreateDefault())
                ?? throw new InvalidOperationException("Failed to deserialize node status.");

            // The /nodes/{node}/status response does not include the node name; populate it.
            if (string.IsNullOrEmpty(status.Name))
                status.Name = Node;

            WriteObject(status);
        }
    }
}
