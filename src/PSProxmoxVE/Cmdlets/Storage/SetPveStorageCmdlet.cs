using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Storage
{
    /// <summary>
    /// <para type="synopsis">Updates a Proxmox VE storage definition.</para>
    /// <para type="description">
    /// Modifies the configuration of an existing storage definition.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveStorage", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveStorageCmdlet : PveCmdletBase
    {
        /// <summary>The storage identifier to update.</summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, HelpMessage = "The storage identifier.")]
        public string Storage { get; set; } = string.Empty;

        /// <summary>Comma-separated list of content types (e.g. 'images,iso,backup').</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated content types (e.g. 'images,iso,backup').")]
        public string? Content { get; set; }

        /// <summary>Comma-separated list of nodes that can access this storage.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of nodes.")]
        public string? Nodes { get; set; }

        /// <summary>Whether the storage is enabled.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Whether the storage is enabled.")]
        public SwitchParameter Enabled { get; set; }

        /// <summary>Whether the storage is shared across nodes.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Whether the storage is shared.")]
        public SwitchParameter Shared { get; set; }

        /// <summary>Additional configuration as a hashtable for parameters not covered by named parameters.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Additional configuration parameters as a hashtable.")]
        public System.Collections.Hashtable? AdditionalConfig { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"storage '{Storage}'", "Set"))
                return;

            var session = GetSession();
            var service = new StorageService();

            var config = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(Content)) config["content"] = Content!;
            if (!string.IsNullOrEmpty(Nodes)) config["nodes"] = Nodes!;
            if (MyInvocation.BoundParameters.ContainsKey("Enabled"))
                config["disable"] = Enabled.IsPresent ? "0" : "1";
            if (MyInvocation.BoundParameters.ContainsKey("Shared"))
                config["shared"] = Shared.IsPresent ? "1" : "0";

            if (AdditionalConfig != null)
            {
                foreach (var key in AdditionalConfig.Keys)
                    config[key.ToString()!] = AdditionalConfig[key]?.ToString() ?? string.Empty;
            }

            WriteVerbose($"Updating storage '{Storage}'...");
            service.UpdateStorage(session, Storage, config);
        }
    }
}
