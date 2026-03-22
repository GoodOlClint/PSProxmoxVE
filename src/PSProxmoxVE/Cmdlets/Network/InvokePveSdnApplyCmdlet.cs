using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Network
{
    /// <summary>
    /// <para type="synopsis">Applies pending SDN configuration changes in Proxmox VE.</para>
    /// <para type="description">
    /// Applies all pending Software-Defined Networking configuration changes cluster-wide.
    /// Run this after making changes with Set-PveSdnZone, Set-PveSdnVnet, etc.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "PveSdnApply", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class InvokePveSdnApplyCmdlet : PveCmdletBase
    {
        protected override void ProcessRecord()
        {
            if (!ShouldProcess("SDN configuration", "Apply pending changes"))
                return;

            var session = GetSession();
            RequireVersion(session, "SDN", 6, 2, 8, 0);
            var service = new NetworkService();

            WriteVerbose("Applying pending SDN configuration changes...");
            service.ApplySdn(session);
        }
    }
}
