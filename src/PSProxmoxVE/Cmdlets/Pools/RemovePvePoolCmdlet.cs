using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Pools
{
    /// <summary>
    /// <para type="synopsis">Removes a resource pool from Proxmox VE.</para>
    /// <para type="description">
    /// Deletes a resource pool from the Proxmox VE cluster configuration.
    /// This operation is destructive and requires confirmation unless -Force is specified.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PvePool",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePvePoolCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The pool ID to remove.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The pool ID to remove.")]
        public string PoolId { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"pool '{PoolId}'", "Remove-PvePool"))
                return;

            var session = GetSession();
            RequireVersion(session, "Pool deletion", 8, 1);
            var service = new PoolService();

            WriteVerbose($"Removing pool '{PoolId}'...");
            service.RemovePool(session, PoolId);
        }
    }
}
