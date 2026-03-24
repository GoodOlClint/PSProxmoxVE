using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Cluster;

/// <summary>
/// Represents the cluster-wide options returned by GET /cluster/options.
/// </summary>
public class PveClusterOptions
{
    /// <summary>
    /// The keyboard layout for the web console (e.g., "en-us", "de").
    /// </summary>
    [JsonProperty("keyboard")]
    public string? Keyboard { get; set; }

    /// <summary>
    /// The default language for the web UI.
    /// </summary>
    [JsonProperty("language")]
    public string? Language { get; set; }

    /// <summary>
    /// The HTTP proxy for outgoing connections (e.g., for downloading templates).
    /// </summary>
    [JsonProperty("http_proxy")]
    public string? HttpProxy { get; set; }

    /// <summary>
    /// The sender email address for cluster notification emails.
    /// </summary>
    [JsonProperty("email_from")]
    public string? EmailFrom { get; set; }

    /// <summary>
    /// The default console viewer (e.g., "applet", "vv", "html5").
    /// </summary>
    [JsonProperty("console")]
    public string? Console { get; set; }

    /// <summary>
    /// The cluster fencing mode (e.g., "watchdog", "hardware", "both").
    /// </summary>
    [JsonProperty("fencing")]
    public string? Fencing { get; set; }

    /// <summary>
    /// The default migration settings (type, network).
    /// </summary>
    [JsonProperty("migration")]
    public string? Migration { get; set; }

    /// <summary>
    /// The MAC address prefix used for auto-generated MAC addresses.
    /// </summary>
    [JsonProperty("mac_prefix")]
    public string? MacPrefix { get; set; }

    /// <summary>
    /// A description or comment for the cluster.
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The maximum number of parallel worker processes for bulk operations.
    /// </summary>
    [JsonProperty("max_workers")]
    public int? MaxWorkers { get; set; }

    /// <summary>
    /// HA manager settings.
    /// </summary>
    [JsonProperty("ha")]
    public string? Ha { get; set; }

    /// <summary>
    /// Bandwidth limit settings for various operations (clone, migration, etc.).
    /// </summary>
    [JsonProperty("bwlimit")]
    public string? BwLimit { get; set; }

    /// <summary>
    /// Settings controlling next VM/CT ID allocation.
    /// </summary>
    [JsonProperty("next-id")]
    public string? NextId { get; set; }

    /// <summary>
    /// Cluster resource scheduling settings.
    /// </summary>
    [JsonProperty("crs")]
    public string? Crs { get; set; }

    /// <summary>
    /// U2F configuration settings.
    /// </summary>
    [JsonProperty("u2f")]
    public string? U2f { get; set; }

    /// <summary>
    /// WebAuthn configuration settings.
    /// </summary>
    [JsonProperty("webauthn")]
    public string? Webauthn { get; set; }

    /// <summary>
    /// Tag style configuration for the web UI.
    /// </summary>
    [JsonProperty("tag-style")]
    public string? TagStyle { get; set; }

    /// <summary>
    /// Notification system configuration.
    /// </summary>
    [JsonProperty("notify")]
    public string? Notify { get; set; }

    /// <summary>
    /// Custom consent text displayed at login.
    /// </summary>
    [JsonProperty("consent-text")]
    public string? ConsentText { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"ClusterOptions: Language={Language ?? "default"} | Console={Console ?? "default"} | Fencing={Fencing ?? "default"}";
    }
}
