using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Converts an LXC container to a template.</para>
    /// <para type="description">
    /// Converts the specified LXC container into a template via the Proxmox VE API.
    /// This operation is irreversible — once converted, the container cannot be started
    /// and can only be used as a base for cloning new containers.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PveContainerTemplate",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(PveTask))]
    public sealed class NewPveContainerTemplateCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the container resides.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to convert to a template.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "New-PveContainerTemplate"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Converting container {VmId} to template on node '{Node}'...");
            var task = containerService.ConvertToTemplate(session, Node, VmId);

            WriteObject(task);
        }
    }
}
