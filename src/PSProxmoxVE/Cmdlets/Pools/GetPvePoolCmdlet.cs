using System.Management.Automation;
using PSProxmoxVE.Core.Models.Cluster;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Pools
{
    /// <summary>
    /// <para type="synopsis">Lists Proxmox VE resource pools.</para>
    /// <para type="description">
    /// Returns resource pool configurations from the Proxmox VE cluster.
    /// Optionally filter by pool ID to retrieve a specific pool with its members.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PvePool")]
    [OutputType(typeof(PvePool))]
    public sealed class GetPvePoolCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The pool ID to retrieve. When omitted, all pools are returned.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The pool ID to retrieve.")]
        public string? PoolId { get; set; }

        protected override void ProcessRecord()
        {
            var session = GetSession();
            var service = new PoolService();

            if (!string.IsNullOrEmpty(PoolId))
            {
                WriteVerbose($"Getting pool '{PoolId}'...");
                var pool = service.GetPool(session, PoolId!);
                WriteObject(pool);
            }
            else
            {
                WriteVerbose("Getting all pools...");
                var pools = service.GetPools(session);
                foreach (var pool in pools)
                {
                    WriteObject(pool);
                }
            }
        }
    }
}
