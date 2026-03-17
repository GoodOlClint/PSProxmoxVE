using System;
using System.Collections.Generic;
using System.Management.Automation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.Nodes;

namespace PSProxmoxVE.Cmdlets.Nodes
{
    /// <summary>
    /// <para type="synopsis">Returns one or more Proxmox VE cluster nodes.</para>
    /// <para type="description">
    /// Get-PveNode retrieves all nodes visible to the authenticated session.
    /// Use -Name to filter to a specific node by exact name.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveNode")]
    [OutputType(typeof(PveNode))]
    public sealed class GetPveNodeCmdlet : PveCmdletBase
    {
        /// <summary>Optional node name filter. When specified, only the matching node is returned.</summary>
        [Parameter(Mandatory = false, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string? Name { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();

            string responseBody;
            try
            {
                using var client = new PveHttpClient(session);
                responseBody = client.GetAsync("/api2/json/nodes").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(
                    ex,
                    "GetPveNodeFailed",
                    ErrorCategory.ConnectionError,
                    session.Hostname));
                return;
            }

            var json = JObject.Parse(responseBody);
            var dataToken = json["data"] ?? throw new InvalidOperationException("Response did not contain a 'data' field.");

            var nodes = dataToken.ToObject<List<PveNode>>(
                JsonSerializer.CreateDefault()) ?? new List<PveNode>();

            foreach (var node in nodes)
            {
                if (Name is null || string.Equals(node.Name, Name, StringComparison.OrdinalIgnoreCase))
                    WriteObject(node);
            }
        }
    }
}
