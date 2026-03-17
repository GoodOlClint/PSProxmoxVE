using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Vms
{
    /// <summary>
    /// Represents a single log line from a Proxmox VE task log,
    /// as returned by the /nodes/{node}/tasks/{upid}/log endpoint.
    /// </summary>
    public class PveTaskLog
    {
        /// <summary>Line number (1-based).</summary>
        [JsonProperty("n")]
        public int LineNumber { get; set; }

        /// <summary>Log line text.</summary>
        [JsonProperty("t")]
        public string Text { get; set; } = string.Empty;

        public override string ToString() => $"{LineNumber,4}: {Text}";
    }
}
