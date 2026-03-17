using System.Management.Automation;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE.Cmdlets.Connection
{
    /// <summary>
    /// <para type="synopsis">Tests whether an active, non-expired Proxmox VE session exists.</para>
    /// <para type="description">
    /// Without -Detailed, returns $true if a session is present and not expired, otherwise $false.
    /// With -Detailed, returns the PveSession object (or nothing if there is no active session).
    /// </para>
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "PveConnection")]
    [OutputType(typeof(bool), ParameterSetName = new[] { "Default" })]
    [OutputType(typeof(PveSession), ParameterSetName = new[] { "Detailed" })]
    public sealed class TestPveConnectionCmdlet : PSCmdlet
    {
        /// <summary>
        /// When specified, writes the PveSession object instead of a boolean.
        /// Nothing is written if no session is active.
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = "Detailed")]
        public SwitchParameter Detailed { get; set; }

        protected override void ProcessRecord()
        {
            var session = ModuleState.ActiveSession;
            var isConnected = session is not null && !session.IsExpired;

            if (Detailed.IsPresent)
            {
                if (isConnected)
                    WriteObject(session);
                // If not connected, write nothing — caller can check $null.
            }
            else
            {
                WriteObject(isConnected);
            }
        }
    }
}
