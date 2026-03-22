using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Containers
{
    /// <summary>
    /// <para type="synopsis">Updates the configuration of an LXC container.</para>
    /// <para type="description">
    /// Modifies one or more configuration settings of the specified LXC container via the
    /// Proxmox VE API. Only the parameters explicitly provided are changed; all other settings
    /// are left untouched.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveContainerConfig", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveContainerConfigCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the container resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The PVE node name.")]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to configure. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The container identifier.")]
        [ValidateRange(100, 999999999)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The hostname to assign to the container.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The hostname for the container.")]
        public string? Hostname { get; set; }

        /// <summary>
        /// <para type="description">Number of CPU cores to allocate to the container.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Number of CPU cores to allocate.")]
        public int? Cores { get; set; }

        /// <summary>
        /// <para type="description">Memory limit in MiB.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Memory limit in MiB.")]
        public int? Memory { get; set; }

        /// <summary>
        /// <para type="description">Swap size in MiB.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Swap size in MiB.")]
        public int? Swap { get; set; }

        /// <summary>
        /// <para type="description">Human-readable description / notes for the container.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Description or notes for the container.")]
        public string? Description { get; set; }

        /// <summary>
        /// <para type="description">Semicolon-separated list of tags to assign to the container.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Semicolon-separated list of tags.")]
        public string? Tags { get; set; }

        /// <summary>
        /// <para type="description">DNS nameservers (space-separated).</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "DNS nameservers (space-separated).")]
        public string? Nameserver { get; set; }

        /// <summary>
        /// <para type="description">DNS search domain.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "DNS search domain.")]
        public string? SearchDomain { get; set; }

        /// <summary>
        /// <para type="description">
        /// Hashtable of additional configuration keys to set. Use this for any PVE config
        /// option not exposed as a named parameter (e.g., net0, mp0, rootfs).
        /// Values are merged after named parameters and can override them.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Extra config keys as a hashtable.")]
        public Hashtable? AdditionalConfig { get; set; }

        /// <summary>
        /// <para type="description">
        /// Comma-separated list of configuration keys to delete.
        /// Maps to the PVE API "delete" parameter.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated config keys to delete.")]
        public string? Delete { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Set-PveContainerConfig"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

            WriteVerbose($"Updating config for container {VmId} on node '{Node}'...");
            var config = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(Hostname))
                config["hostname"] = Hostname!;
            if (Cores.HasValue)
                config["cores"] = Cores.Value.ToString();
            if (Memory.HasValue)
                config["memory"] = Memory.Value.ToString();
            if (Swap.HasValue)
                config["swap"] = Swap.Value.ToString();
            if (!string.IsNullOrEmpty(Description))
                config["description"] = Description!;
            if (!string.IsNullOrEmpty(Tags))
                config["tags"] = Tags!;
            if (!string.IsNullOrEmpty(Nameserver))
                config["nameserver"] = Nameserver!;
            if (!string.IsNullOrEmpty(SearchDomain))
                config["searchdomain"] = SearchDomain!;

            if (AdditionalConfig != null)
            {
                foreach (DictionaryEntry entry in AdditionalConfig)
                    config[entry.Key.ToString()!] = entry.Value ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(Delete))
                config["delete"] = Delete!;

            containerService.SetContainerConfig(session, Node, VmId, config);
        }
    }
}
