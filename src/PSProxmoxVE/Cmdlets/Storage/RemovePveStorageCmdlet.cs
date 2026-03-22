using System.Management.Automation;
using PSProxmoxVE.Core.Client;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Removes a storage definition from Proxmox VE.</para>
    /// <para type="description">
    /// Deletes the specified storage definition from the Proxmox VE cluster configuration.
    /// This does not delete the underlying data — only the storage reference is removed.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "PveStorage",
        SupportsShouldProcess = true,
        ConfirmImpact = ConfirmImpact.High)]
    [OutputType(typeof(void))]
    public sealed class RemovePveStorageCmdlet : PveCmdletBase
    {
        /// <summary>The storage identifier to remove.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The storage pool name.")]
        public string Storage { get; set; } = string.Empty;

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Storage, "Remove PVE Storage"))
                return;

            var session = GetSession();
            using var client = new PveHttpClient(session);

            WriteVerbose($"Removing storage '{Storage}'...");
            client.DeleteAsync($"storage/{Storage}").GetAwaiter().GetResult();
        }
    }
}
