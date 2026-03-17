using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PSProxmoxVE.Core.Models.Users;

/// <summary>
/// Represents a Proxmox VE role as returned by the /access/roles endpoint.
/// Roles are named collections of privileges that can be assigned to users or groups via ACLs.
/// </summary>
public class PveRole
{
    /// <summary>
    /// The unique role identifier (e.g., "Administrator", "PVEVMAdmin", "PVEDatastoreUser").
    /// </summary>
    [JsonPropertyName("roleid")]
    [JsonProperty("roleid")]
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of privilege strings assigned to this role
    /// (e.g., "VM.Allocate,VM.Config.Disk,Datastore.AllocateSpace").
    /// </summary>
    [JsonPropertyName("privs")]
    [JsonProperty("privs")]
    public string? Privileges { get; set; }

    /// <summary>
    /// Indicates whether this is a built-in / special role (1) that cannot be deleted,
    /// or a custom role (0).
    /// </summary>
    [JsonPropertyName("special")]
    [JsonProperty("special")]
    public int? Special { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var specialStr = Special is 1 ? " [built-in]" : string.Empty;
        return $"Role: {RoleId}{specialStr} | Privs: {Privileges ?? "none"}";
    }
}
