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
    public sealed class SetPveContainerConfigCmdlet : PveCmdletBase
    {
        /// <summary>
        /// <para type="description">
        /// The node on which the container resides. Accepts pipeline input from a PveNode object's Name property.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// <para type="description">The ID of the container to configure. Accepts pipeline input.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int VmId { get; set; }

        /// <summary>
        /// <para type="description">The hostname to assign to the container.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Hostname { get; set; }

        /// <summary>
        /// <para type="description">Number of CPU cores to allocate to the container.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? Cores { get; set; }

        /// <summary>
        /// <para type="description">Memory limit in MiB.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? Memory { get; set; }

        /// <summary>
        /// <para type="description">Swap size in MiB.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? Swap { get; set; }

        /// <summary>
        /// <para type="description">Human-readable description / notes for the container.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Description { get; set; }

        /// <summary>
        /// <para type="description">Semicolon-separated list of tags to assign to the container.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Tags { get; set; }

        /// <summary>
        /// <para type="description">DNS nameservers (space-separated).</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? Nameserver { get; set; }

        /// <summary>
        /// <para type="description">DNS search domain.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public string? SearchDomain { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess($"Container {VmId} on node '{Node}'", "Set-PveContainerConfig"))
                return;

            var session = GetSession();
            var containerService = new ContainerService();

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

            containerService.SetContainerConfig(session, Node, VmId, config);
        }
    }
}
