using System;
using System.Management.Automation;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE.Cmdlets.Connection
{
    /// <summary>
    /// <para type="synopsis">Establishes an authenticated session to a Proxmox VE server.</para>
    /// <para type="description">
    /// Connect-PveServer authenticates against the Proxmox VE API using either a
    /// PSCredential (username/password) or a pre-generated API token, and stores the
    /// resulting session in module state for use by subsequent cmdlets.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Connect, "PveServer",
        DefaultParameterSetName = ParameterSetCredential)]
    [Alias("cpve")]
    [OutputType(typeof(PveSession))]
    public sealed class ConnectPveServerCmdlet : PSCmdlet
    {
        private const string ParameterSetCredential = "Credential";
        private const string ParameterSetApiToken   = "ApiToken";

        /// <summary>Hostname or IP address of the Proxmox VE server.</summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "Hostname or IP of the Proxmox VE server.")]
        [ValidateNotNullOrEmpty]
        public string Server { get; set; } = string.Empty;

        /// <summary>API port. Defaults to 8006.</summary>
        [Parameter(Mandatory = false, HelpMessage = "API port. Defaults to 8006.")]
        [ValidateRange(1, 65535)]
        public int Port { get; set; } = 8006;

        /// <summary>Username and password credential. Username must include a realm, e.g. root@pam.</summary>
        [Parameter(Mandatory = true, ParameterSetName = ParameterSetCredential, HelpMessage = "Username and password. Username must include realm (e.g. root@pam).")]
        [ValidateNotNull]
        public PSCredential? Credential { get; set; }

        /// <summary>
        /// Proxmox VE API token in the format USER@REALM!TOKENID=UUID,
        /// e.g. root@pam!mytoken=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = ParameterSetApiToken, HelpMessage = "API token in USER@REALM!TOKENID=UUID format.")]
        [ValidateNotNullOrEmpty]
        public string? ApiToken { get; set; }

        /// <summary>When specified, skips TLS certificate validation for the server.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Skip TLS certificate validation.")]
        public SwitchParameter SkipCertificateCheck { get; set; }

        /// <summary>When specified, writes the resulting PveSession object to the pipeline.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Output the session object to the pipeline.")]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            PveSession session;

            switch (ParameterSetName)
            {
                case ParameterSetCredential:
                {
                    if (Credential is null)
                        ThrowTerminatingError(new ErrorRecord(
                            new ArgumentNullException(nameof(Credential)),
                            "CredentialRequired",
                            ErrorCategory.InvalidArgument,
                            null));

                    var username = Credential!.UserName;
                    var password = Credential.GetNetworkCredential().Password;

                    try
                    {
                        session = PveAuthenticator.AuthenticateWithCredentials(
                            Server, Port, SkipCertificateCheck.IsPresent, username, password);
                    }
                    catch (Exception ex)
                    {
                        ThrowTerminatingError(new ErrorRecord(
                            ex,
                            "PveAuthenticationFailed",
                            ErrorCategory.AuthenticationError,
                            Server));
                        return; // unreachable — satisfies compiler
                    }
                    break;
                }

                case ParameterSetApiToken:
                {
                    try
                    {
                        session = PveAuthenticator.AuthenticateWithApiToken(
                            Server, Port, SkipCertificateCheck.IsPresent, ApiToken!);
                    }
                    catch (Exception ex)
                    {
                        ThrowTerminatingError(new ErrorRecord(
                            ex,
                            "PveAuthenticationFailed",
                            ErrorCategory.AuthenticationError,
                            Server));
                        return; // unreachable — satisfies compiler
                    }
                    break;
                }

                default:
                    ThrowTerminatingError(new ErrorRecord(
                        new InvalidOperationException($"Unknown parameter set: {ParameterSetName}"),
                        "UnknownParameterSet",
                        ErrorCategory.InvalidOperation,
                        null));
                    return;
            }

            ModuleState.ActiveSession = session;

            if (SkipCertificateCheck.IsPresent)
                WriteWarning("TLS certificate validation is disabled for this session. Connections are susceptible to man-in-the-middle attacks. Use only in trusted networks or test environments.");

            WriteVerbose($"Connected to {Server}:{Port} as {session.AuthMode} (PVE {session.ServerVersion}).");

            if (PassThru.IsPresent)
                WriteObject(session);
        }
    }
}
