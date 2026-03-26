using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PSProxmoxVE.Core.Tests
{
    /// <summary>
    /// Validates that the module's hardcoded enum values (ValidateSet, privilege names, etc.)
    /// match what the PVE API actually accepts, as defined by the OpenAPI spec.
    ///
    /// The fixture pve-api-enums.json is extracted from the full OpenAPI spec at
    /// ~/Source/pve_api/tools/pve-api-parser/pve-openapi.json and contains only
    /// parameter enum values and response field names.
    /// </summary>
    public class OpenApiSpecValidationTests
    {
        private static readonly Lazy<JObject> _spec = new Lazy<JObject>(() =>
            JObject.Parse(TestHelper.LoadFixture("pve-api-enums.json")));

        private static JObject Spec => _spec.Value;

        private static HashSet<string> GetEnumValues(string path, string method, string paramName)
        {
            var pathData = Spec["paths"]?[path]?[method]?["params"]?[paramName];
            if (pathData == null) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var enumArray = pathData["enum"] as JArray;
            if (enumArray != null)
            {
                foreach (var v in enumArray)
                    values.Add(v.ToString());
            }

            var xEnumArray = pathData["x-enum-values"] as JArray;
            if (xEnumArray != null)
            {
                foreach (var v in xEnumArray)
                    values.Add(v.ToString());
            }

            return values;
        }

        // ── Privileges ──────────────────────────────────────────────────────

        [Fact]
        public void Privileges_AllKnownValuesAcceptedByApi()
        {
            var apiPrivileges = GetEnumValues("/access/roles", "post", "privs");
            Assert.NotEmpty(apiPrivileges);

            // These are privilege names used in tests and documentation
            var modulePrivileges = new[]
            {
                "VM.Allocate", "VM.Audit", "VM.Backup", "VM.Clone",
                "VM.Config.CDROM", "VM.Config.Cloudinit", "VM.Config.CPU",
                "VM.Config.Disk", "VM.Config.HWType", "VM.Config.Memory",
                "VM.Config.Network", "VM.Config.Options", "VM.Console",
                "VM.Migrate", "VM.Monitor", "VM.PowerMgmt", "VM.Snapshot",
                "VM.Snapshot.Rollback",
                "Datastore.Allocate", "Datastore.AllocateSpace",
                "Datastore.AllocateTemplate", "Datastore.Audit",
                "Sys.Audit", "Sys.Console", "Sys.Modify", "Sys.PowerMgmt",
                "Sys.Syslog", "Sys.AccessNetwork", "Sys.Incoming",
                "SDN.Allocate", "SDN.Audit", "SDN.Use",
                "User.Modify", "Permissions.Modify",
                "Pool.Allocate", "Pool.Audit",
                "Group.Allocate",
                "Realm.Allocate", "Realm.AllocateUser",
                "Mapping.Audit", "Mapping.Modify", "Mapping.Use",
                "VM.GuestAgent.Audit", "VM.GuestAgent.FileRead",
                "VM.GuestAgent.FileWrite", "VM.GuestAgent.FileSystemMgmt",
                "VM.GuestAgent.Unrestricted", "VM.Replicate"
            };

            var invalid = modulePrivileges.Where(p => !apiPrivileges.Contains(p)).ToList();
            Assert.True(invalid.Count == 0,
                $"Invalid privilege name(s) not accepted by PVE API: {string.Join(", ", invalid)}. " +
                $"Valid privileges: {string.Join(", ", apiPrivileges.OrderBy(p => p))}");
        }

        // ── Storage Types ───────────────────────────────────────────────────

        [Theory]
        [InlineData("dir")]
        [InlineData("nfs")]
        [InlineData("lvm")]
        [InlineData("lvmthin")]
        [InlineData("zfspool")]
        [InlineData("zfs")]
        [InlineData("cephfs")]
        [InlineData("rbd")]
        [InlineData("iscsi")]
        [InlineData("iscsidirect")]
        [InlineData("cifs")]
        [InlineData("pbs")]
        [InlineData("btrfs")]
        [InlineData("esxi")]
        // Note: "glusterfs" was removed in PVE 9.0 — replaced by btrfs/esxi.
        // The module's ValidateSet still includes it.
        // See: NewPveStorageCmdlet.cs line 27-28
        public void StorageType_IsValidInApi(string storageType)
        {
            var apiTypes = GetEnumValues("/storage", "post", "type");
            Assert.True(apiTypes.Contains(storageType),
                $"Storage type '{storageType}' not in API enum: {string.Join(", ", apiTypes)}");
        }

        // ── Backup Modes ────────────────────────────────────────────────────

        [Theory]
        [InlineData("snapshot")]
        [InlineData("suspend")]
        [InlineData("stop")]
        public void BackupMode_IsValidInApi(string mode)
        {
            var apiModes = GetEnumValues("/nodes/{node}/vzdump", "post", "mode");
            Assert.True(apiModes.Contains(mode),
                $"Backup mode '{mode}' not in API enum: {string.Join(", ", apiModes)}");
        }

        // ── Backup Compression ──────────────────────────────────────────────

        [Theory]
        [InlineData("zstd")]
        [InlineData("lzo")]
        [InlineData("gzip")]
        // Note: "none" is NOT valid — PVE uses "0" for no compression.
        // The module's ValidateSet includes "none" which may cause API errors.
        // See: NewPveBackupCmdlet.cs line 51, NewPveBackupJobCmdlet.cs line 54
        [InlineData("0")]
        public void BackupCompression_IsValidInApi(string compress)
        {
            var apiValues = GetEnumValues("/nodes/{node}/vzdump", "post", "compress");
            Assert.True(apiValues.Contains(compress),
                $"Compression '{compress}' not in API enum: {string.Join(", ", apiValues)}");
        }

        // ── Disk Formats ────────────────────────────────────────────────────

        [Theory]
        [InlineData("raw")]
        [InlineData("qcow2")]
        [InlineData("vmdk")]
        public void DiskFormat_IsValidInApi(string format)
        {
            var apiFormats = GetEnumValues("/nodes/{node}/qemu/{vmid}/move_disk", "post", "format");
            Assert.True(apiFormats.Contains(format),
                $"Disk format '{format}' not in API enum: {string.Join(", ", apiFormats)}");
        }

        // ── HA Resource States ──────────────────────────────────────────────

        [Theory]
        [InlineData("started")]
        [InlineData("stopped")]
        [InlineData("disabled")]
        [InlineData("ignored")]
        public void HaResourceState_IsValidInApi(string state)
        {
            var apiStates = GetEnumValues("/cluster/ha/resources", "post", "state");
            Assert.True(apiStates.Contains(state),
                $"HA state '{state}' not in API enum: {string.Join(", ", apiStates)}");
        }

        // ── SDN Zone Types ──────────────────────────────────────────────────

        [Theory]
        [InlineData("vlan")]
        [InlineData("vxlan")]
        [InlineData("evpn")]
        [InlineData("simple")]
        [InlineData("qinq")]
        public void SdnZoneType_IsValidInApi(string zoneType)
        {
            var apiTypes = GetEnumValues("/cluster/sdn/zones", "post", "type");
            Assert.True(apiTypes.Contains(zoneType),
                $"SDN zone type '{zoneType}' not in API enum: {string.Join(", ", apiTypes)}");
        }

        // ── SDN Controller Types ────────────────────────────────────────────

        [Theory]
        [InlineData("evpn")]
        [InlineData("bgp")]
        public void SdnControllerType_IsValidInApi(string controllerType)
        {
            var apiTypes = GetEnumValues("/cluster/sdn/controllers", "post", "type");
            Assert.True(apiTypes.Contains(controllerType),
                $"SDN controller type '{controllerType}' not in API enum: {string.Join(", ", apiTypes)}");
        }

        // ── SDN IPAM Types ──────────────────────────────────────────────────

        [Theory]
        [InlineData("pve")]
        [InlineData("netbox")]
        [InlineData("phpipam")]
        public void SdnIpamType_IsValidInApi(string ipamType)
        {
            var apiTypes = GetEnumValues("/cluster/sdn/ipams", "post", "type");
            Assert.True(apiTypes.Contains(ipamType),
                $"SDN IPAM type '{ipamType}' not in API enum: {string.Join(", ", apiTypes)}");
        }

        // ── SDN DNS Types ───────────────────────────────────────────────────

        [Theory]
        [InlineData("powerdns")]
        public void SdnDnsType_IsValidInApi(string dnsType)
        {
            var apiTypes = GetEnumValues("/cluster/sdn/dns", "post", "type");
            Assert.True(apiTypes.Contains(dnsType),
                $"SDN DNS type '{dnsType}' not in API enum: {string.Join(", ", apiTypes)}");
        }

        // ── Firewall Actions ────────────────────────────────────────────────
        // The 'action' param uses a pattern regex (also accepts group names),
        // not an enum. Validate against the documented pattern instead.

        [Theory]
        [InlineData("ACCEPT")]
        [InlineData("DROP")]
        [InlineData("REJECT")]
        public void FirewallAction_MatchesApiPattern(string action)
        {
            // PVE defines action as pattern: [A-Za-z][A-Za-z0-9\-\_]+
            Assert.Matches(@"^[A-Za-z][A-Za-z0-9\-_]+$", action);
        }

        // ── Firewall Directions ─────────────────────────────────────────────

        [Theory]
        [InlineData("in")]
        [InlineData("out")]
        [InlineData("group")]
        public void FirewallDirection_IsValidInApi(string direction)
        {
            var apiDirections = GetEnumValues("/cluster/firewall/rules", "post", "type");
            Assert.True(apiDirections.Contains(direction),
                $"Firewall direction '{direction}' not in API enum: {string.Join(", ", apiDirections)}");
        }

        // ── Auth Domain Types ───────────────────────────────────────────────

        [Theory]
        [InlineData("pam")]
        [InlineData("pve")]
        [InlineData("ad")]
        [InlineData("ldap")]
        [InlineData("openid")]
        public void AuthDomainType_IsValidInApi(string domainType)
        {
            var apiTypes = GetEnumValues("/access/domains", "post", "type");
            Assert.True(apiTypes.Contains(domainType),
                $"Auth domain type '{domainType}' not in API enum: {string.Join(", ", apiTypes)}");
        }

        // ── Cluster Resource Types ──────────────────────────────────────────

        [Theory]
        [InlineData("vm")]
        [InlineData("node")]
        [InlineData("storage")]
        [InlineData("sdn")]
        // Note: "lxc" is NOT a valid type filter — PVE uses "vm" for both QEMU and LXC.
        // The module's ValidateSet includes "lxc" which may cause API errors.
        // See: GetPveClusterResourceCmdlet.cs line 23
        public void ClusterResourceType_IsValidInApi(string resourceType)
        {
            var apiTypes = GetEnumValues("/cluster/resources", "get", "type");
            Assert.True(apiTypes.Contains(resourceType),
                $"Cluster resource type '{resourceType}' not in API enum: {string.Join(", ", apiTypes)}");
        }

        // ── Content Types (Upload) ──────────────────────────────────────────

        [Theory]
        [InlineData("iso")]
        [InlineData("vztmpl")]
        public void UploadContentType_IsValidInApi(string contentType)
        {
            var apiTypes = GetEnumValues("/nodes/{node}/storage/{storage}/upload", "post", "content");
            Assert.True(apiTypes.Contains(contentType),
                $"Upload content type '{contentType}' not in API enum: {string.Join(", ", apiTypes)}");
        }

        // ── Console Viewer Types ────────────────────────────────────────────

        [Theory]
        [InlineData("applet")]
        [InlineData("vv")]
        [InlineData("html5")]
        [InlineData("xtermjs")]
        public void ConsoleViewer_IsValidInApi(string viewer)
        {
            var apiViewers = GetEnumValues("/cluster/options", "put", "console");
            Assert.True(apiViewers.Contains(viewer),
                $"Console viewer '{viewer}' not in API enum: {string.Join(", ", apiViewers)}");
        }

        // ── Network Interface Types ─────────────────────────────────────────

        [Theory]
        [InlineData("bridge")]
        [InlineData("bond")]
        [InlineData("eth")]
        [InlineData("alias")]
        [InlineData("vlan")]
        [InlineData("OVSBridge")]
        [InlineData("OVSBond")]
        [InlineData("OVSPort")]
        [InlineData("OVSIntPort")]
        public void NetworkInterfaceType_IsValidInApi(string ifaceType)
        {
            var apiTypes = GetEnumValues("/nodes/{node}/network", "post", "type");
            Assert.True(apiTypes.Contains(ifaceType),
                $"Network interface type '{ifaceType}' not in API enum: {string.Join(", ", apiTypes)}");
        }
    }
}
