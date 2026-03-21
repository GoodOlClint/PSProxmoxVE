using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Resizes a disk attached to an LXC container.</para>
    /// <para type="description">
    /// Resizes the specified disk/volume on an LXC container via the Proxmox VE API.
    /// Use an absolute size (e.g., "50G") to set a fixed size, or a relative size
    /// prefixed with "+" (e.g., "+5G") to grow the disk by that amount.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Resize, "PveContainerDisk", SupportsShouldProcess = true)]
    [OutputType(typeof(PveTask))]
    public sealed class ResizePveContainerDiskCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The node on which the container resides.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container whose disk should be resized.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The disk/volume to resize (e.g., "rootfs", "mp0").</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The disk/volume to resize (e.g. rootfs, mp0).")]
        public string Disk { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">
        /// The new size for the disk. Use an absolute value (e.g., "50G") to set a specific size,
        /// or a "+" prefix (e.g., "+5G") to grow the disk by the specified amount.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "New disk size (e.g. 50G or +5G to grow).")]
        public string Size { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Disk '{Disk}' on container {VmId} (node '{Node}') to size '{Size}'", "Resize-PveContainerDisk"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Resizing disk '{Disk}' on container {VmId}...");
            var task = containerService.ResizeContainerDisk(session, Node, VmId, Disk, Size);

            WriteObject(task);
        }
    }
}
