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
    }
}
