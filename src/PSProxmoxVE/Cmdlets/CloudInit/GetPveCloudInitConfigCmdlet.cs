using System.Management.Automation;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.CloudInit
{
    /// <summary>
    /// <para type="synopsis">Retrieves cloud-init configuration for a Proxmox VE virtual machine.</para>
    /// <para type="description">
    /// Returns the VM configuration with a focus on cloud-init fields: ciuser, cipassword,
    /// sshkeys, ipconfig*, nameserver, and searchdomain. Returns the full PveVmConfig object
    /// so all other config fields are available as well. VmId can be piped from Get-PveVm.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PveCloudInitConfig")]
    [OutputType(typeof(PveVmConfig))]
    public sealed class GetPveCloudInitConfigCmdlet : PveCmdletBase
    {
        /// <summary>The Proxmox VE node name.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>The VM identifier. Accepts pipeline input from Get-PveVm (PveVm.VmId).</summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            RequireVersion(session, "Cloud-Init management", 7, 2);
            var service = new CloudInitService();

            WriteVerbose($"Getting cloud-init config for VM {VmId}...");
            var config = service.GetFullVmConfig(session, Node, VmId);
            WriteObject(config);
        }
    }
}
