using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Users
{
    /// <summary>
    /// <para type="synopsis">Removes a Proxmox VE authentication domain/realm.</para>
    /// <para type="description">
    /// Deletes the specified authentication domain (realm) from the Proxmox VE
    /// access management system.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveDomain",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveDomainCmdlet : PveCmdletBase
    {
        /// <summary>The realm identifier to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The realm identifier to remove.")]
        [ValidateNotNullOrEmpty]
        public string Realm { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Realm, "Remove PVE Domain"))
                return;

            var session = GetSession();
            var service = new UserService();

            WriteVerbose($"Removing domain '{Realm}'...");
            service.RemoveDomain(session, Realm);
        }
    }
}
