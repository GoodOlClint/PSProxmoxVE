using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Removes a Proxmox VE group.</para>
    /// <para type="description">
    /// Deletes the specified group from the Proxmox VE access management system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveGroup",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveGroupCmdlet : PveCmdletBase
    {
        /// <summary>The group identifier to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The group identifier to remove.")]
        [ValidateNotNullOrEmpty]
        public string GroupId { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(GroupId, "Remove PVE Group"))
                return;

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Removing group '{GroupId}'...");
            service.RemoveGroup(session, GroupId);
        }
    }
}
