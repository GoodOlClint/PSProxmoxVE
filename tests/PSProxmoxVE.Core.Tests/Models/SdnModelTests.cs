using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Network;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class SdnModelTests
    {
        [Fact]
        public void PveSdnZone_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_zones.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var zones = data.ToObject<PveSdnZone[]>();
            Assert.NotNull(zones);
            Assert.Equal(2, zones.Length);
        }

        [Fact]
        public void PveSdnZone_Deserialize_Pve9_FirstZone_HasCorrectId()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_zones.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var zones = data.ToObject<PveSdnZone[]>();
            Assert.NotNull(zones);
            Assert.Equal("localzone", zones[0].Zone);
        }

        [Fact]
        public void PveSdnZone_Deserialize_Pve9_FirstZone_HasCorrectType()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_zones.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var zones = data.ToObject<PveSdnZone[]>();
            Assert.NotNull(zones);
            Assert.Equal("simple", zones[0].Type);
        }

        [Fact]
        public void PveSdnZone_Deserialize_Pve9_SecondZone_HasCorrectType()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_zones.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var zones = data.ToObject<PveSdnZone[]>();
            Assert.NotNull(zones);
            Assert.Equal("vxlanzone", zones[1].Zone);
            Assert.Equal("vxlan", zones[1].Type);
        }

        [Fact]
        public void PveSdnZone_Deserialize_Pve9_OptionalFields_NullWhenMissing()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_zones.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var zones = data.ToObject<PveSdnZone[]>();
            Assert.NotNull(zones);
            // Comments field is not present in the fixture
            Assert.Null(zones[0].Comments);
        }

        [Fact]
        public void PveSdnVnet_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_vnets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vnets = data.ToObject<PveSdnVnet[]>();
            Assert.NotNull(vnets);
            Assert.Equal(2, vnets.Length);
        }

        [Fact]
        public void PveSdnVnet_Deserialize_Pve9_FirstVnet_HasCorrectId()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_vnets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vnets = data.ToObject<PveSdnVnet[]>();
            Assert.NotNull(vnets);
            Assert.Equal("myvnet", vnets[0].Vnet);
        }

        [Fact]
        public void PveSdnVnet_Deserialize_Pve9_FirstVnet_HasCorrectZone()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_vnets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vnets = data.ToObject<PveSdnVnet[]>();
            Assert.NotNull(vnets);
            Assert.Equal("localzone", vnets[0].Zone);
        }

        [Fact]
        public void PveSdnVnet_Deserialize_Pve9_FirstVnet_HasTag()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_vnets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vnets = data.ToObject<PveSdnVnet[]>();
            Assert.NotNull(vnets);
            Assert.Equal(100, vnets[0].Tag);
        }

        [Fact]
        public void PveSdnVnet_Deserialize_Pve9_FirstVnet_HasAlias()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_vnets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vnets = data.ToObject<PveSdnVnet[]>();
            Assert.NotNull(vnets);
            Assert.Equal("My VNET", vnets[0].Alias);
        }

        [Fact]
        public void PveSdnVnet_Deserialize_Pve9_SecondVnet_HasCorrectZoneAndTag()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_vnets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vnets = data.ToObject<PveSdnVnet[]>();
            Assert.NotNull(vnets);
            Assert.Equal("prodnet", vnets[1].Vnet);
            Assert.Equal("vxlanzone", vnets[1].Zone);
            Assert.Equal(200, vnets[1].Tag);
        }

        [Fact]
        public void PveSdnVnet_Deserialize_Pve9_OptionalFields_NullWhenMissing()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_vnets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vnets = data.ToObject<PveSdnVnet[]>();
            Assert.NotNull(vnets);
            // Comments not present in fixture entries
            Assert.Null(vnets[0].Comments);
        }

        // ── PveSdnIpam ─────────────────────────────────────────────────

        [Fact]
        public void PveSdnIpam_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_ipams.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var ipams = data.ToObject<PveSdnIpam[]>();
            Assert.NotNull(ipams);
            Assert.Equal(2, ipams.Length);
        }

        [Fact]
        public void PveSdnIpam_Deserialize_Pve9_FirstIpam_IsPveType()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_ipams.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var ipams = data.ToObject<PveSdnIpam[]>();
            Assert.NotNull(ipams);
            Assert.Equal("pve", ipams[0].Ipam);
            Assert.Equal("pve", ipams[0].Type);
        }

        [Fact]
        public void PveSdnIpam_Deserialize_Pve9_FirstIpam_OptionalFieldsAreNull()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_ipams.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var ipams = data.ToObject<PveSdnIpam[]>();
            Assert.NotNull(ipams);
            Assert.Null(ipams[0].Url);
            Assert.Null(ipams[0].Token);
            Assert.Null(ipams[0].Section);
        }

        [Fact]
        public void PveSdnIpam_Deserialize_Pve9_SecondIpam_HasNetboxProperties()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_ipams.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var ipams = data.ToObject<PveSdnIpam[]>();
            Assert.NotNull(ipams);
            Assert.Equal("netbox1", ipams[1].Ipam);
            Assert.Equal("netbox", ipams[1].Type);
            Assert.Equal("https://netbox.example.com", ipams[1].Url);
            Assert.Equal("abc123", ipams[1].Token);
            Assert.Equal(1, ipams[1].Section);
        }

        // ── PveSdnDns ──────────────────────────────────────────────────

        [Fact]
        public void PveSdnDns_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_dns.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var dnsEntries = data.ToObject<PveSdnDns[]>();
            Assert.NotNull(dnsEntries);
            Assert.Single(dnsEntries);
        }

        [Fact]
        public void PveSdnDns_Deserialize_Pve9_FirstEntry_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_dns.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var dnsEntries = data.ToObject<PveSdnDns[]>();
            Assert.NotNull(dnsEntries);
            Assert.Equal("powerdns1", dnsEntries[0].Dns);
            Assert.Equal("powerdns", dnsEntries[0].Type);
            Assert.Equal("http://dns.example.com:8081", dnsEntries[0].Url);
            Assert.Equal("secret123", dnsEntries[0].Key);
            Assert.Equal(3600, dnsEntries[0].Ttl);
        }

        [Fact]
        public void PveSdnDns_Deserialize_Pve9_FirstEntry_OptionalFieldsAreNull()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_dns.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var dnsEntries = data.ToObject<PveSdnDns[]>();
            Assert.NotNull(dnsEntries);
            Assert.Null(dnsEntries[0].ReverseMaskV6);
        }

        // ── PveSdnController ───────────────────────────────────────────

        [Fact]
        public void PveSdnController_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_controllers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var controllers = data.ToObject<PveSdnController[]>();
            Assert.NotNull(controllers);
            Assert.Single(controllers);
        }

        [Fact]
        public void PveSdnController_Deserialize_Pve9_FirstController_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_sdn_controllers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var controllers = data.ToObject<PveSdnController[]>();
            Assert.NotNull(controllers);
            Assert.Equal("evpn1", controllers[0].Controller);
            Assert.Equal("evpn", controllers[0].Type);
            Assert.Equal(65000, controllers[0].Asn);
            Assert.Equal("10.0.0.1,10.0.0.2", controllers[0].Peers);
            Assert.Equal("pve1", controllers[0].Node);
        }
    }
}
