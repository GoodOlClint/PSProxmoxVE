using System.Management.Automation;
using PSProxmoxVE.Core.Models.Containers;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Gets the configuration of an LXC container.</para>
    /// <para type="description">
    /// Retrieves the full configuration of the specified LXC container from the Proxmox VE API,
    /// including CPU, memory, storage, network, and metadata settings.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveContainerConfig")]
    [OutputType(typeof(PveContainerConfig))]
    public sealed class GetPveContainerConfigCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the container resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container whose configuration to retrieve. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Getting config for container {VmId} on node '{Node}'...");
            var config = containerService.GetContainerConfig(session, Node, VmId);
            WriteObject(config);
        }
    }
}
