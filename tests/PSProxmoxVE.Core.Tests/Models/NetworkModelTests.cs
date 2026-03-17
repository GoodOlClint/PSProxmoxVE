using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Network;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class NetworkModelTests
    {
        [Fact]
        public void PveNetwork_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_networks.json");
            var data = JObject.Parse(json)["data"];
            var networks = data.ToObject<PveNetwork[]>();
            Assert.NotNull(networks);
            Assert.Equal(2, networks.Length);
        }

        [Fact]
        public void PveNetwork_Deserialize_Pve9_FirstInterface_HasCorrectName()
        {
            var json = TestHelper.LoadFixture("pve9_networks.json");
            var data = JObject.Parse(json)["data"];
            var networks = data.ToObject<PveNetwork[]>();
            Assert.NotNull(networks);
            Assert.Equal("vmbr0", networks[0].Iface);
        }

        [Fact]
        public void PveNetwork_Deserialize_Pve9_FirstInterface_HasCorrectType()
        {
            var json = TestHelper.LoadFixture("pve9_networks.json");
            var data = JObject.Parse(json)["data"];
            var networks = data.ToObject<PveNetwork[]>();
            Assert.NotNull(networks);
            Assert.Equal("bridge", networks[0].Type);
        }

        [Fact]
        public void PveNetwork_Deserialize_Pve9_FirstInterface_HasCorrectAddress()
        {
            var json = TestHelper.LoadFixture("pve9_networks.json");
            var data = JObject.Parse(json)["data"];
            var networks = data.ToObject<PveNetwork[]>();
            Assert.NotNull(networks);
            Assert.Equal("192.168.1.10", networks[0].Address);
            Assert.Equal("255.255.255.0", networks[0].Netmask);
            Assert.Equal("192.168.1.1", networks[0].Gateway);
        }

        [Fact]
        public void PveNetwork_Deserialize_Pve9_FirstInterface_HasCidr()
        {
            var json = TestHelper.LoadFixture("pve9_networks.json");
            var data = JObject.Parse(json)["data"];
            var networks = data.ToObject<PveNetwork[]>();
            Assert.NotNull(networks);
            Assert.Equal("192.168.1.10/24", networks[0].Cidr);
        }

        [Fact]
        public void PveNetwork_Deserialize_Pve9_FirstInterface_HasBridgePorts()
        {
            var json = TestHelper.LoadFixture("pve9_networks.json");
            var data = JObject.Parse(json)["data"];
            var networks = data.ToObject<PveNetwork[]>();
            Assert.NotNull(networks);
            Assert.Equal("enp0s31f6", networks[0].BridgePorts);
        }

        [Fact]
        public void PveNetwork_Deserialize_Pve9_FirstInterface_Autostart_IsEnabled()
        {
            var json = TestHelper.LoadFixture("pve9_networks.json");
            var data = JObject.Parse(json)["data"];
            var networks = data.ToObject<PveNetwork[]>();
            Assert.NotNull(networks);
            Assert.Equal(1, networks[0].Autostart);
        }

        [Fact]
        public void PveNetwork_Deserialize_Pve9_OptionalFields_NullWhenMissing()
        {
            var json = TestHelper.LoadFixture("pve9_networks.json");
            var data = JObject.Parse(json)["data"];
            var networks = data.ToObject<PveNetwork[]>();
            Assert.NotNull(networks);
            // No VLAN on primary bridge
            Assert.Null(networks[0].VlanId);
            Assert.Null(networks[0].Comments);
        }

        [Fact]
        public void PveNetwork_Deserialize_Pve9_SecondInterface_HasCorrectAddress()
        {
            var json = TestHelper.LoadFixture("pve9_networks.json");
            var data = JObject.Parse(json)["data"];
            var networks = data.ToObject<PveNetwork[]>();
            Assert.NotNull(networks);
            Assert.Equal("vmbr1", networks[1].Iface);
            Assert.Equal("10.0.0.1/24", networks[1].Cidr);
        }
    }
}
