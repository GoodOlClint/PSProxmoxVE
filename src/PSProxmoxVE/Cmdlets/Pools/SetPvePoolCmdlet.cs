using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Pools
{
    /// <summary>
    /// <para type="synopsis">Updates a resource pool in Proxmox VE.</para>
    /// <para type="description">
    /// Modifies an existing resource pool configuration. You can update the comment,
    /// add members, or remove members.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PvePool", SupportsShouldProcess = true)]
    public sealed class SetPvePoolCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">The pool ID to update.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The pool ID to update.")]
        public string PoolId { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">An optional comment or description for the pool.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "An optional comment or description for the pool.")]
        public string? Comment { get; set; }

        /// <summary>
        /// <para type="description">Comma-separated list of members to add (e.g., "100,200,storage1").</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of members to add (e.g., '100,200,storage1').")]
        public string? Members { get; set; }

        /// <summary>
        /// <para type="description">Comma-separated list of members to remove.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of members to remove.")]
        public string? Delete { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"pool '{PoolId}'", "Set-PvePool"))
                return;

            var session = GetSession();
            RequireVersion(session, "Pool update", 8, 1);
            var service = new PoolService();

            var config = new Dictionary<string, string>();
            if (Comment != null)
                config["comment"] = Comment;
            if (!string.IsNullOrEmpty(Members))
                config["members"] = Members!;
            if (!string.IsNullOrEmpty(Delete))
                config["delete"] = Delete!;

            WriteVerbose($"Updating pool '{PoolId}'...");
            service.UpdatePool(session, PoolId, config);
        }
    }
}
