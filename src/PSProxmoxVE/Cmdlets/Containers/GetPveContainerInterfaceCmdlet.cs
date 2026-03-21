using System.Management.Automation;
using PSProxmoxVE.Core.Models.Containers;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Gets network interface information for an LXC container.</para>
    /// <para type="description">
    /// Retrieves network interface information for the specified LXC container
    /// via the Proxmox VE API, including interface names, MAC addresses, and IP addresses.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveContainerInterface")]
    [OutputType(typeof(PveContainerInterface))]
    public sealed class GetPveContainerInterfaceCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The container identifier.</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "Container interface listing", 8, 1);

            WriteVerbose($"Getting network interfaces for container {VmId}...");
            var service = new ContainerService();
            var interfaces = service.GetInterfaces(session, Node, VmId);

            foreach (var iface in interfaces)
                WriteObject(iface);
        }
    }
}
