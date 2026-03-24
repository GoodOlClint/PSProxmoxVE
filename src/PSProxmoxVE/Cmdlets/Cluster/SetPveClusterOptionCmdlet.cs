using System.Collections.Generic;
using System.Management.Automation;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Cmdlets.Cluster
{
    /// <summary>
    /// <para type="synopsis">Sets cluster-wide datacenter options.</para>
    /// <para type="description">
    /// Updates datacenter-level cluster options such as keyboard layout, language,
    /// migration settings, console preference, and other global settings.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PveClusterOption", SupportsShouldProcess = true)]
    [OutputType(typeof(void))]
    public sealed class SetPveClusterOptionCmdlet : PveCmdletBase
    {
        /// <summary>Default keyboard layout for VNC.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Default keyboard layout for VNC.")]
        [ValidateSet("de", "de-ch", "da", "en-gb", "en-us", "es", "fi", "fr", "fr-be",
            "fr-ca", "fr-ch", "hu", "is", "it", "ja", "lt", "mk", "nl", "no", "pl",
            "pt", "pt-br", "sv", "sl", "tr")]
        public string? Keyboard { get; set; }

        /// <summary>Default GUI language.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Default GUI language.")]
        public string? Language { get; set; }

        /// <summary>Default console viewer.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Default console viewer.")]
        [ValidateSet("applet", "vv", "html5", "xtermjs")]
        public string? Console { get; set; }

        /// <summary>Email address for notifications.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Email address to send notifications from.")]
        public string? EmailFrom { get; set; }

        /// <summary>External HTTP proxy URL.</summary>
        [Parameter(Mandatory = false, HelpMessage = "External HTTP proxy URL for downloads.")]
        public string? HttpProxy { get; set; }

        /// <summary>MAC address prefix for auto-generated guest MACs.</summary>
        [Parameter(Mandatory = false, HelpMessage = "MAC address prefix for virtual guests.")]
        public string? MacPrefix { get; set; }

        /// <summary>Max workers per node for bulk operations.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Max workers per node for bulk operations like stopall.")]
        [ValidateRange(1, int.MaxValue)]
        public int? MaxWorkers { get; set; }

        /// <summary>Fencing mode.</summary>
        [Parameter(Mandatory = false, HelpMessage = "HA fencing mode.")]
        [ValidateSet("watchdog", "hardware", "both")]
        public string? Fencing { get; set; }

        /// <summary>Migration settings.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Cluster-wide migration settings string.")]
        public string? Migration { get; set; }

        /// <summary>Datacenter description.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Datacenter description shown in the web UI.")]
        public string? Description { get; set; }

        /// <summary>Settings to delete.</summary>
        [Parameter(Mandatory = false, HelpMessage = "Comma-separated list of settings to delete/reset.")]
        public string? Delete { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess("cluster options", "Set"))
                return;

            var session = GetSession();
            var service = new ClusterConfigService();

            var data = new Dictionary<string, string>();
            if (Keyboard != null) data["keyboard"] = Keyboard;
            if (Language != null) data["language"] = Language;
            if (Console != null) data["console"] = Console;
            if (EmailFrom != null) data["email_from"] = EmailFrom;
            if (HttpProxy != null) data["http_proxy"] = HttpProxy;
            if (MacPrefix != null) data["mac_prefix"] = MacPrefix;
            if (MaxWorkers.HasValue) data["max_workers"] = MaxWorkers.Value.ToString();
            if (Fencing != null) data["fencing"] = Fencing;
            if (Migration != null) data["migration"] = Migration;
            if (Description != null) data["description"] = Description;
            if (Delete != null) data["delete"] = Delete;

            WriteVerbose("Setting cluster options...");
            service.SetClusterOptions(session, data);
        }
    }
}
