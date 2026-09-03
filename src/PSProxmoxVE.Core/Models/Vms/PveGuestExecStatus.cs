using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Utilities;

namespace PSProxmoxVE.Core.Models.Vms;

/// <summary>
/// Represents the status of a guest-agent command started via exec, as returned
/// by the /nodes/{node}/qemu/{vmid}/agent/exec-status endpoint.
/// </summary>
public class PveGuestExecStatus
{
    /// <summary>
    /// Whether the command has exited yet. PVE has been observed sending this as
    /// a boolean, an integer (1/0), or a string ("1"/"0"); <see cref="TolerantBooleanConverter"/>
    /// normalizes all three.
    /// </summary>
    [JsonProperty("exited")]
    [JsonConverter(typeof(TolerantBooleanConverter))]
    public bool? Exited { get; set; }

    /// <summary>
    /// Process exit code, if it was normally terminated.
    /// </summary>
    [JsonProperty("exitcode")]
    public int? ExitCode { get; set; }

    /// <summary>
    /// Signal number or exception code, if the process was abnormally terminated.
    /// </summary>
    [JsonProperty("signal")]
    public int? Signal { get; set; }

    /// <summary>
    /// Base64-encoded stdout of the process.
    /// </summary>
    [JsonProperty("out-data")]
    public string? OutData { get; set; }

    /// <summary>
    /// Base64-encoded stderr of the process.
    /// </summary>
    [JsonProperty("err-data")]
    public string? ErrData { get; set; }

    /// <summary>
    /// True if stdout was not fully captured.
    /// </summary>
    [JsonProperty("out-truncated")]
    public bool? OutTruncated { get; set; }

    /// <summary>
    /// True if stderr was not fully captured.
    /// </summary>
    [JsonProperty("err-truncated")]
    public bool? ErrTruncated { get; set; }

    /// <summary>
    /// Raw landing spot for any key not mapped to a typed property above.
    /// </summary>
    [JsonExtensionData]
    private IDictionary<string, JToken>? ExtensionData { get; set; }

    private Dictionary<string, object?>? _additionalProperties;

    /// <summary>
    /// Any keys not surfaced as a typed property above. Keys map to native
    /// .NET values so the dictionary works naturally in PowerShell pipelines.
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object?> AdditionalProperties =>
        _additionalProperties ??= ExtensionData == null
            ? new Dictionary<string, object?>()
            : ExtensionData.ToDictionary(kvp => kvp.Key, kvp => JsonHelper.ToNative(kvp.Value));

    /// <inheritdoc />
    public override string ToString()
    {
        return Exited == true
            ? $"Exec Status | Exited, code {ExitCode?.ToString() ?? "N/A"}"
            : "Exec Status | Running";
    }
}
