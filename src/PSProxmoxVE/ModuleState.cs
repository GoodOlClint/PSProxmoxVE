using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE
{
    /// <summary>Module-scoped state container for the active PVE session</summary>
    internal static class ModuleState
    {
        internal static PveSession? ActiveSession { get; set; }
    }
}
