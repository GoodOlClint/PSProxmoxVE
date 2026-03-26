using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PSProxmoxVE.Core.Tests
{
    /// <summary>
    /// Validates that the module's hardcoded enum values (ValidateSet, privilege names, etc.)
    /// match what the PVE API actually accepts, as defined by per-version OpenAPI specs.
    ///
    /// Fixtures pve-api-enums.pve{7,8,9}.json are extracted from the full OpenAPI specs at
    /// ~/Source/pve_api/tools/pve-api-parser/ and contain parameter enum values per PVE version.
    ///
    /// PVE 7 = best-effort support, PVE 8 + 9 = fully supported.
    /// </summary>
    public class OpenApiSpecValidationTests
    {
        private static readonly Lazy<JObject> _specPve7 = new Lazy<JObject>(() =>
            JObject.Parse(TestHelper.LoadFixture("pve-api-enums.pve7.json")));
        private static readonly Lazy<JObject> _specPve8 = new Lazy<JObject>(() =>
            JObject.Parse(TestHelper.LoadFixture("pve-api-enums.pve8.json")));
        private static readonly Lazy<JObject> _specPve9 = new Lazy<JObject>(() =>
            JObject.Parse(TestHelper.LoadFixture("pve-api-enums.pve9.json")));

        private static JObject SpecPve7 => _specPve7.Value;
        private static JObject SpecPve8 => _specPve8.Value;
        private static JObject SpecPve9 => _specPve9.Value;

        private static HashSet<string> GetEnumValues(JObject spec, string path, string method, string paramName)
        {
            var pathData = spec["paths"]?[path]?[method]?["params"]?[paramName];
            if (pathData == null) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (pathData["enum"] is JArray enumArray)
                foreach (var v in enumArray)
                    values.Add(v.ToString());

            if (pathData["x-enum-values"] is JArray xEnumArray)
                foreach (var v in xEnumArray)
                    values.Add(v.ToString());

            return values;
        }

        /// <summary>
        /// Asserts that a value is valid in at least one of the supported PVE versions.
        /// Returns which versions accept/reject it for diagnostic purposes.
        /// </summary>
        private static void AssertValidInAnyVersion(string path, string method, string paramName, string value)
        {
            var pve7 = GetEnumValues(SpecPve7, path, method, paramName);
            var pve8 = GetEnumValues(SpecPve8, path, method, paramName);
            var pve9 = GetEnumValues(SpecPve9, path, method, paramName);

            Assert.True(
                pve7.Contains(value) || pve8.Contains(value) || pve9.Contains(value),
                $"'{value}' not valid in any PVE version for {method.ToUpper()} {path} param '{paramName}'. " +
                $"PVE 7: [{string.Join(", ", pve7)}], PVE 8: [{string.Join(", ", pve8)}], PVE 9: [{string.Join(", ", pve9)}]");
        }

        /// <summary>
        /// Asserts that a value is valid in ALL fully-supported PVE versions (8 + 9).
        /// </summary>
        private static void AssertValidInAllSupported(string path, string method, string paramName, string value)
        {
            var pve8 = GetEnumValues(SpecPve8, path, method, paramName);
            var pve9 = GetEnumValues(SpecPve9, path, method, paramName);

            var missing = new List<string>();
            if (!pve8.Contains(value)) missing.Add("PVE 8");
            if (!pve9.Contains(value)) missing.Add("PVE 9");

            Assert.True(missing.Count == 0,
                $"'{value}' not valid in: {string.Join(", ", missing)} for {method.ToUpper()} {path} param '{paramName}'. " +
                $"PVE 8: [{string.Join(", ", pve8)}], PVE 9: [{string.Join(", ", pve9)}]");
        }

        // ── Privileges ──────────────────────────────────────────────────────

        [Fact]
        public void Privileges_AllPve8And9Common()
        {
            var pve8 = GetEnumValues(SpecPve8, "/access/roles", "post", "privs");
            var pve9 = GetEnumValues(SpecPve9, "/access/roles", "post", "privs");
            Assert.NotEmpty(pve8);
            Assert.NotEmpty(pve9);

            var common = pve8.Intersect(pve9, StringComparer.Ordinal).OrderBy(p => p).ToList();
            Assert.True(common.Count > 30, $"Expected >30 common privileges, got {common.Count}");
        }

        [Fact]
        public void Privileges_VmMonitor_OnlyPve7And8()
        {
            var pve7 = GetEnumValues(SpecPve7, "/access/roles", "post", "privs");
            var pve8 = GetEnumValues(SpecPve8, "/access/roles", "post", "privs");
            var pve9 = GetEnumValues(SpecPve9, "/access/roles", "post", "privs");

            Assert.Contains("VM.Monitor", pve7);
            Assert.Contains("VM.Monitor", pve8);
            Assert.DoesNotContain("VM.Monitor", pve9);
        }

        [Fact]
        public void Privileges_Pve9Only()
        {
            var pve8 = GetEnumValues(SpecPve8, "/access/roles", "post", "privs");
            var pve9 = GetEnumValues(SpecPve9, "/access/roles", "post", "privs");

            // These privileges were added in PVE 9
            var guestAgentPrivs = new[]
            {
                "VM.GuestAgent.Audit", "VM.GuestAgent.FileRead", "VM.GuestAgent.FileWrite",
                "VM.GuestAgent.FileSystemMgmt", "VM.GuestAgent.Unrestricted",
                "VM.Replicate"
            };

            foreach (var priv in guestAgentPrivs)
            {
                Assert.DoesNotContain(priv, pve8);
                Assert.Contains(priv, pve9);
            }
        }

        [Theory]
        [InlineData("VM.Allocate")]
        [InlineData("VM.Audit")]
        [InlineData("VM.Backup")]
        [InlineData("VM.Clone")]
        [InlineData("VM.Config.CDROM")]
        [InlineData("VM.Config.Cloudinit")]
        [InlineData("VM.Config.CPU")]
        [InlineData("VM.Config.Disk")]
        [InlineData("VM.Config.HWType")]
        [InlineData("VM.Config.Memory")]
        [InlineData("VM.Config.Network")]
        [InlineData("VM.Config.Options")]
        [InlineData("VM.Console")]
        [InlineData("VM.Migrate")]
        [InlineData("VM.PowerMgmt")]
        [InlineData("VM.Snapshot")]
        [InlineData("VM.Snapshot.Rollback")]
        [InlineData("Datastore.Allocate")]
        [InlineData("Datastore.AllocateSpace")]
        [InlineData("Datastore.AllocateTemplate")]
        [InlineData("Datastore.Audit")]
        [InlineData("Sys.Audit")]
        [InlineData("Sys.Console")]
        [InlineData("Sys.Modify")]
        [InlineData("Sys.PowerMgmt")]
        [InlineData("Sys.Syslog")]
        [InlineData("Sys.AccessNetwork")]
        [InlineData("Sys.Incoming")]
        [InlineData("SDN.Allocate")]
        [InlineData("SDN.Audit")]
        [InlineData("SDN.Use")]
        [InlineData("User.Modify")]
        [InlineData("Permissions.Modify")]
        [InlineData("Pool.Allocate")]
        [InlineData("Pool.Audit")]
        [InlineData("Group.Allocate")]
        [InlineData("Realm.Allocate")]
        [InlineData("Realm.AllocateUser")]
        public void Privilege_ValidInAllSupportedVersions(string privilege)
        {
            AssertValidInAllSupported("/access/roles", "post", "privs", privilege);
        }

        [Theory]
        [InlineData("Mapping.Audit")]
        [InlineData("Mapping.Modify")]
        [InlineData("Mapping.Use")]
        public void Privilege_Pve8AndAbove(string privilege)
        {
            var pve7 = GetEnumValues(SpecPve7, "/access/roles", "post", "privs");
            var pve8 = GetEnumValues(SpecPve8, "/access/roles", "post", "privs");
            var pve9 = GetEnumValues(SpecPve9, "/access/roles", "post", "privs");

            Assert.DoesNotContain(privilege, pve7);
            Assert.Contains(privilege, pve8);
            Assert.Contains(privilege, pve9);
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
        public void StorageType_ValidInAllSupportedVersions(string storageType)
        {
            AssertValidInAllSupported("/storage", "post", "type", storageType);
        }

        [Theory]
        [InlineData("btrfs")]
        [InlineData("esxi")]
        public void StorageType_ValidAcrossAllVersions(string storageType)
        {
            var pve7 = GetEnumValues(SpecPve7, "/storage", "post", "type");
            var pve8 = GetEnumValues(SpecPve8, "/storage", "post", "type");
            var pve9 = GetEnumValues(SpecPve9, "/storage", "post", "type");
            Assert.Contains(storageType, pve7);
            Assert.Contains(storageType, pve8);
            Assert.Contains(storageType, pve9);
        }

        [Fact]
        public void StorageType_GlusterfsNotInAnyVersion()
        {
            // glusterfs was removed before PVE 7 — should not be in any ValidateSet
            var pve7 = GetEnumValues(SpecPve7, "/storage", "post", "type");
            var pve8 = GetEnumValues(SpecPve8, "/storage", "post", "type");
            var pve9 = GetEnumValues(SpecPve9, "/storage", "post", "type");

            Assert.DoesNotContain("glusterfs", pve7);
            Assert.DoesNotContain("glusterfs", pve8);
            Assert.DoesNotContain("glusterfs", pve9);
        }

        // ── Backup Modes ────────────────────────────────────────────────────

        [Theory]
        [InlineData("snapshot")]
        [InlineData("suspend")]
        [InlineData("stop")]
        public void BackupMode_ValidInAllSupportedVersions(string mode)
        {
            AssertValidInAllSupported("/nodes/{node}/vzdump", "post", "mode", mode);
        }

        // ── Backup Compression ──────────────────────────────────────────────

        [Theory]
        [InlineData("zstd")]
        [InlineData("lzo")]
        [InlineData("gzip")]
        [InlineData("0")]
        public void BackupCompression_ValidInAllSupportedVersions(string compress)
        {
            AssertValidInAllSupported("/nodes/{node}/vzdump", "post", "compress", compress);
        }

        // ── Disk Formats ────────────────────────────────────────────────────

        [Theory]
        [InlineData("raw")]
        [InlineData("qcow2")]
        [InlineData("vmdk")]
        public void DiskFormat_ValidInAllSupportedVersions(string format)
        {
            AssertValidInAllSupported("/nodes/{node}/qemu/{vmid}/move_disk", "post", "format", format);
        }

        // ── HA Resource States ──────────────────────────────────────────────

        [Theory]
        [InlineData("started")]
        [InlineData("stopped")]
        [InlineData("disabled")]
        [InlineData("ignored")]
        public void HaResourceState_ValidInAllSupportedVersions(string state)
        {
            AssertValidInAllSupported("/cluster/ha/resources", "post", "state", state);
        }

        // ── SDN Zone Types ──────────────────────────────────────────────────

        [Theory]
        [InlineData("vlan")]
        [InlineData("vxlan")]
        [InlineData("evpn")]
        [InlineData("simple")]
        [InlineData("qinq")]
        public void SdnZoneType_ValidInAllSupportedVersions(string zoneType)
        {
            AssertValidInAllSupported("/cluster/sdn/zones", "post", "type", zoneType);
        }

        // ── SDN Controller Types ────────────────────────────────────────────

        [Theory]
        [InlineData("evpn")]
        [InlineData("bgp")]
        public void SdnControllerType_ValidInAllSupportedVersions(string controllerType)
        {
            AssertValidInAllSupported("/cluster/sdn/controllers", "post", "type", controllerType);
        }

        // ── SDN IPAM Types ──────────────────────────────────────────────────

        [Theory]
        [InlineData("pve")]
        [InlineData("netbox")]
        [InlineData("phpipam")]
        public void SdnIpamType_ValidInAllSupportedVersions(string ipamType)
        {
            AssertValidInAllSupported("/cluster/sdn/ipams", "post", "type", ipamType);
        }

        // ── SDN DNS Types ───────────────────────────────────────────────────

        [Theory]
        [InlineData("powerdns")]
        public void SdnDnsType_ValidInAllSupportedVersions(string dnsType)
        {
            AssertValidInAllSupported("/cluster/sdn/dns", "post", "type", dnsType);
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
        public void FirewallDirection_ValidInAllSupportedVersions(string direction)
        {
            AssertValidInAllSupported("/cluster/firewall/rules", "post", "type", direction);
        }

        // ── Auth Domain Types ───────────────────────────────────────────────

        [Theory]
        [InlineData("pam")]
        [InlineData("pve")]
        [InlineData("ad")]
        [InlineData("ldap")]
        [InlineData("openid")]
        public void AuthDomainType_ValidInAllSupportedVersions(string domainType)
        {
            AssertValidInAllSupported("/access/domains", "post", "type", domainType);
        }

        // ── Cluster Resource Types ──────────────────────────────────────────

        [Theory]
        [InlineData("vm")]
        [InlineData("node")]
        [InlineData("storage")]
        [InlineData("sdn")]
        public void ClusterResourceType_ValidInAllSupportedVersions(string resourceType)
        {
            AssertValidInAllSupported("/cluster/resources", "get", "type", resourceType);
        }

        // ── Content Types (Upload) ──────────────────────────────────────────

        [Theory]
        [InlineData("iso")]
        [InlineData("vztmpl")]
        public void UploadContentType_ValidInAllSupportedVersions(string contentType)
        {
            AssertValidInAllSupported("/nodes/{node}/storage/{storage}/upload", "post", "content", contentType);
        }

        // ── Console Viewer Types ────────────────────────────────────────────

        [Theory]
        [InlineData("applet")]
        [InlineData("vv")]
        [InlineData("html5")]
        [InlineData("xtermjs")]
        public void ConsoleViewer_ValidInAnyVersion(string viewer)
        {
            // 'applet' was removed in later versions — validate across any
            AssertValidInAnyVersion("/cluster/options", "put", "console", viewer);
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
        public void NetworkInterfaceType_ValidInAllSupportedVersions(string ifaceType)
        {
            AssertValidInAllSupported("/nodes/{node}/network", "post", "type", ifaceType);
        }

        // ── Version Progression ─────────────────────────────────────────────

        [Fact]
        public void Pve9_HasMorePrivilegesThanPve8()
        {
            var pve8 = GetEnumValues(SpecPve8, "/access/roles", "post", "privs");
            var pve9 = GetEnumValues(SpecPve9, "/access/roles", "post", "privs");
            Assert.True(pve9.Count > pve8.Count,
                $"Expected PVE 9 ({pve9.Count}) to have more privileges than PVE 8 ({pve8.Count})");
        }

        [Fact]
        public void Pve9_HasMorePathsThanPve8()
        {
            var pve8Paths = SpecPve8["paths"]?.Children().Count() ?? 0;
            var pve9Paths = SpecPve9["paths"]?.Children().Count() ?? 0;
            Assert.True(pve9Paths > pve8Paths,
                $"Expected PVE 9 ({pve9Paths}) to have more paths than PVE 8 ({pve8Paths})");
        }
    }
}
