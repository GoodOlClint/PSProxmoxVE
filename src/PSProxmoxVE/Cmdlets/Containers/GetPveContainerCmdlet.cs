using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using PSProxmoxVE.Core.Models.Containers;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Gets one or more LXC containers from a Proxmox VE server.</para>
    /// <para type="description">
    /// Retrieves container objects from the Proxmox VE API. Results can be filtered by
    /// node, container ID, name, status, or tag.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveContainer")]
    [OutputType(typeof(PveContainer))]
    public sealed class GetPveContainerCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The name of the node to query. Accepts pipeline input from a PveNode object's Name property.
        /// When omitted, containers from all nodes are returned.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public string? Node { get; set; }

        /// <summary>
        /// <para type="description">Filter results to the container with this ID.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? VmId { get; set; }

        /// <summary>
        /// <para type="description">Filter results to containers whose name matches this value (case-insensitive, contains match).</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Name { get; set; }

        /// <summary>
        /// <para type="description">Filter results to containers in the specified status (e.g., "running", "stopped").</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Status { get; set; }

        /// <summary>
        /// <para type="description">Filter results to containers that have the specified tag (substring match against the semicolon-separated tags field).</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Tag { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new ContainerService();

            IEnumerable<PveContainer> containers = service.GetContainers(session, Node);

            if (VmId.HasValue)
                containers = containers.Where(c => c.VmId == VmId.Value);

            if (!string.IsNullOrEmpty(Name))
                containers = containers.Where(c => c.Name != null &&
                    c.Name.IndexOf(Name, System.StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrEmpty(Status))
                containers = containers.Where(c => string.Equals(c.Status, Status, System.StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(Tag))
                containers = containers.Where(c => c.Tags != null &&
                    c.Tags.IndexOf(Tag, System.StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var container in containers)
                WriteObject(container);
        }
    }
}
