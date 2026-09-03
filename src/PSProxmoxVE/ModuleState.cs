using System.Management.Automation;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE
{
    /// <summary>
    /// Per-runspace storage for the session established by Connect-PveServer.
    /// The value lives in the module's own session state, which PowerShell creates
    /// once per runspace, so runspaces cannot see or overwrite each other's session.
    /// </summary>
    internal static class ModuleState
    {
        internal const string ActiveSessionVariable = "PSProxmoxVE.ActiveSession";

        // Qualified so a nested scope in the module's session state (InModuleScope, a
        // module-scoped scriptblock) neither writes a copy that dies with the scope nor reads
        // through to a same-named global.
        private const string ModuleActiveSessionVariable = "script:" + ActiveSessionVariable;

        // Importing the assembly directly rather than through the manifest yields a module
        // with no session state; the global scope is then the only per-runspace slot left.
        private const string GlobalActiveSessionVariable = "global:" + ActiveSessionVariable;

        internal static PveSession? GetActiveSession(PSCmdlet cmdlet)
        {
            var moduleState = ModuleSessionState(cmdlet);
            return moduleState is null
                ? cmdlet.SessionState.PSVariable.GetValue(GlobalActiveSessionVariable) as PveSession
                : moduleState.PSVariable.GetValue(ModuleActiveSessionVariable) as PveSession;
        }

        internal static void SetActiveSession(PSCmdlet cmdlet, PveSession? session)
        {
            var moduleState = ModuleSessionState(cmdlet);
            if (moduleState is null)
                cmdlet.SessionState.PSVariable.Set(GlobalActiveSessionVariable, session);
            else
                moduleState.PSVariable.Set(ModuleActiveSessionVariable, session);
        }

        // PSCmdlet.SessionState resolves in the caller's scope, so it would store the session
        // wherever the cmdlet happened to be invoked from.
        private static SessionState? ModuleSessionState(PSCmdlet cmdlet) =>
            cmdlet.MyInvocation?.MyCommand?.Module?.SessionState;
    }
}
