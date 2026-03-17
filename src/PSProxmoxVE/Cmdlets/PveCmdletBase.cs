using System.Management.Automation;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Exceptions;

namespace PSProxmoxVE.Cmdlets
{
    /// <summary>
    /// Base class for all PSProxmoxVE cmdlets. Provides optional -Session parameter
    /// and a helper method to resolve and validate the active session.
    /// </summary>
    public abstract class PveCmdletBase : PSCmdlet
    {
        /// <summary>
        /// An explicit PveSession to use for this cmdlet invocation.
        /// When omitted, the module-level session stored by Connect-PveServer is used.
        /// </summary>
        [Parameter(Mandatory = false)]
        public PveSession? Session { get; set; }

        /// <summary>
        /// Returns the session to use for this cmdlet.
        /// Resolution order: -Session parameter → ModuleState.ActiveSession.
        /// Throws <see cref="PveNotConnectedException"/> if no session is available,
        /// or <see cref="PveSessionExpiredException"/> if the session ticket has expired.
        /// </summary>
        protected PveSession GetSession()
        {
            var session = Session ?? ModuleState.ActiveSession;

            if (session is null)
                throw new PveNotConnectedException();

            if (session.IsExpired)
                throw new PveSessionExpiredException();

            return session;
        }
    }
}
