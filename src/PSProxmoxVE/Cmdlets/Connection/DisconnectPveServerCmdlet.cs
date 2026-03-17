using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Connection
{
    /// <summary>
    /// <para type="synopsis">Closes the active Proxmox VE session.</para>
    /// <para type="description">
    /// Disconnect-PveServer invalidates the module-level session. For ticket-based
    /// sessions it additionally attempts a best-effort DELETE against the
    /// /access/ticket endpoint to invalidate the server-side ticket. Errors from
    /// that request are silently ignored so that the local session is always cleared.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Disconnect, "PveServer",
        SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public sealed class DisconnectPveServerCmdlet : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            var session = ModuleState.ActiveSession;

            if (session is null)
            {
                WriteWarning("No active Proxmox VE session to disconnect.");
                return;
            }

            if (!ShouldProcess($"{session.Hostname}:{session.Port}", "Disconnect"))
                return;

            // Best-effort server-side ticket invalidation for ticket-based auth.
            if (session.AuthMode == PveAuthMode.Ticket)
            {
                try
                {
                    using var client = new PveHttpClient(session);
                    client.DeleteAsync("/api2/json/access/ticket").GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    // Non-fatal — we still clear the local session below.
                    WriteVerbose($"Server-side ticket invalidation failed (ignored): {ex.Message}");
                }
            }

            ModuleState.ActiveSession = null;

            WriteVerbose($"Disconnected from {session.Hostname}:{session.Port}.");
        }
    }
}
