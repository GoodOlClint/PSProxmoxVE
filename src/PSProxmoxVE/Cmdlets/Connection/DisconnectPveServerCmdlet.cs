using System.Management.Automation;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE.Cmdlets.Connection
{
    /// <summary>
    /// <para type="synopsis">Discards the local Proxmox VE session.</para>
    /// <para type="description">
    /// Disconnect-PveServer discards the module-level session from memory. PVE tickets cannot be
    /// revoked server-side and expire on their own (typically two hours).
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Disconnect, "PveServer",
        SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(void))]
    [Alias("dpve")]
    public sealed class DisconnectPveServerCmdlet : PveCmdletBase
    {
        protected override void ProcessPveRecord()
        {
            bool explicitSessionSupplied = MyInvocation.BoundParameters.ContainsKey(nameof(Session));
            var sessionToDisconnect = explicitSessionSupplied ? Session : ModuleState.ActiveSession;

            if (sessionToDisconnect is null)
            {
                WriteWarning("No active Proxmox VE session to disconnect.");
                return;
            }

            if (!ReferenceEquals(sessionToDisconnect, ModuleState.ActiveSession))
            {
                var lifecycle = sessionToDisconnect.AuthMode == PveAuthMode.ApiToken
                    ? "API tokens do not expire; revoke it with Remove-PveApiToken if it is no longer needed."
                    : "PVE tickets cannot be revoked and expire on their own.";
                WriteWarning($"The supplied session for {sessionToDisconnect.Hostname}:{sessionToDisconnect.Port} is not the module-level session; nothing was changed. Discard the variable. {lifecycle}");
                return;
            }

            if (!ShouldProcess($"{sessionToDisconnect.Hostname}:{sessionToDisconnect.Port}", "Disconnect"))
                return;

            ModuleState.ActiveSession = null;
            WriteVerbose($"Disconnected from {sessionToDisconnect.Hostname}:{sessionToDisconnect.Port}.");
        }
    }
}
