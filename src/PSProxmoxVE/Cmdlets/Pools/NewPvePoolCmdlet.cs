using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Pools
{
    /// <summary>
    /// <para type="synopsis">Creates a new resource pool in Proxmox VE.</para>
    /// <para type="description">
    /// Creates a new resource pool in the Proxmox VE cluster with the specified ID
    /// and optional comment.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PvePool", SupportsShouldProcess = true)]
    public sealed class NewPvePoolCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The pool ID to create.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The pool ID to create.")]
        public string PoolId { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">An optional comment or description for the pool.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "An optional comment or description for the pool.")]
        public string? Comment { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"pool '{PoolId}'", "New-PvePool"))
                return;

            var session = GetSession();
            var service = new PoolService();

            WriteVerbose($"Creating pool '{PoolId}'...");
            service.CreatePool(session, PoolId, Comment);
        }
    }
}
