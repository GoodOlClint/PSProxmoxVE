using System.Management.Automation;

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
        protected override void ProcessRecord()
        {
            var sessionToDisconnect = Session ?? ModuleState.ActiveSession;

            if (sessionToDisconnect is null)
            {
                WriteWarning("No active Proxmox VE session to disconnect.");
                return;
            }

            if (!ShouldProcess($"{sessionToDisconnect.Hostname}:{sessionToDisconnect.Port}", "Disconnect"))
                return;

            if (Session is null || sessionToDisconnect == ModuleState.ActiveSession)
            {
                ModuleState.ActiveSession = null;
            }

            WriteVerbose($"Disconnected from {sessionToDisconnect.Hostname}:{sessionToDisconnect.Port}.");
        }
    }
}
