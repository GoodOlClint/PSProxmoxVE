using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Nodes;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class NodeModelTests
    {
        [Fact]
        public void PveNode_Deserialize_Pve9()
        {
            var json = TestHelper.LoadFixture("pve9_nodes.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var nodes = data.ToObject<PveNode[]>();
            Assert.NotNull(nodes);
            Assert.Equal(2, nodes.Length);
            Assert.Equal("pve1", nodes[0].Name);
            Assert.Equal("online", nodes[0].Status);
            Assert.Equal(16, nodes[0].CpuCount);
            Assert.Equal(68719476736L, nodes[0].MemoryTotal);
        }

        [Fact]
        public void PveNode_Deserialize_Pve9_SecondNode()
        {
            var json = TestHelper.LoadFixture("pve9_nodes.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var nodes = data.ToObject<PveNode[]>();
            Assert.NotNull(nodes);
            Assert.Equal("pve2", nodes[1].Name);
            Assert.Equal("online", nodes[1].Status);
            Assert.Equal(8, nodes[1].CpuCount);
            Assert.Equal(34359738368L, nodes[1].MemoryTotal);
        }

        [Fact]
        public void PveNode_Deserialize_Pve8()
        {
            var json = TestHelper.LoadFixture("pve8_nodes.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var nodes = data.ToObject<PveNode[]>();
            Assert.NotNull(nodes);
            Assert.Single(nodes);
            Assert.Equal("pve-old", nodes[0].Name);
        }

        [Fact]
        public void PveNode_Deserialize_Pve8_HasExpectedCpuAndMemory()
        {
            var json = TestHelper.LoadFixture("pve8_nodes.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var nodes = data.ToObject<PveNode[]>();
            Assert.NotNull(nodes);
            Assert.Equal(4, nodes[0].CpuCount);
            Assert.Equal(16106127360L, nodes[0].MemoryTotal);
        }

        [Fact]
        public void PveNodeStatus_Deserialize_Pve9()
        {
            var json = TestHelper.LoadFixture("pve9_node_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var status = data.ToObject<PveNodeStatus>();
            Assert.NotNull(status);
            Assert.Equal(0.125, status.CpuUsage);
        }

        [Fact]
        public void PveNodeStatus_Deserialize_Pve9_HasMemoryAndUptime()
        {
            var json = TestHelper.LoadFixture("pve9_node_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var status = data.ToObject<PveNodeStatus>();
            Assert.NotNull(status);
            Assert.Equal("pve1", status.Node);
            Assert.Equal(68719476736L, status.MemoryTotal);
            Assert.Equal(17179869184L, status.MemoryUsed);
            Assert.Equal(864000L, status.Uptime);
        }

        [Fact]
        public void PveNodeStatus_MemoryUsage_IsCalculated()
        {
            var json = TestHelper.LoadFixture("pve9_node_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var status = data.ToObject<PveNodeStatus>();
            Assert.NotNull(status);
            Assert.NotNull(status.MemoryUsage);
            // 17179869184 / 68719476736 * 100 = 25.0
            Assert.Equal(25.0, status.MemoryUsage!.Value, precision: 5);
        }

        [Fact]
        public void PveNode_LoadAverage_IsDeserializedAsArray()
        {
            var json = TestHelper.LoadFixture("pve9_nodes.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var nodes = data.ToObject<PveNode[]>();
            Assert.NotNull(nodes);
            Assert.NotNull(nodes[0].LoadAverage);
            Assert.Equal(3, nodes[0].LoadAverage!.Length);
        }

        [Fact]
        public void PveNodeConfig_Deserialize_HasDocumentedFields()
        {
            var json = @"{""data"": {
                ""description"": ""Primary node"",
                ""wakeonlan"": ""AA:BB:CC:DD:EE:FF"",
                ""ballooning-target"": 80,
                ""startall-onboot-delay"": 30,
                ""digest"": ""abc123""
            }}";
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var config = data.ToObject<PveNodeConfig>();
            Assert.NotNull(config);
            Assert.Equal("Primary node", config.Description);
            Assert.Equal("AA:BB:CC:DD:EE:FF", config.WakeOnLan);
            Assert.Equal(80, config.BallooningTarget);
            Assert.Equal(30, config.StartAllOnbootDelay);
            Assert.Equal("abc123", config.Digest);
        }

        [Fact]
        public void PveNodeConfig_UnmappedKey_LandsInAdditionalProperties()
        {
            var json = @"{""data"": {""description"": ""n1"", ""acmedomain0"": ""example.com,plugin=dns""}}";
            var data = JObject.Parse(json)["data"];
            var config = data!.ToObject<PveNodeConfig>();
            Assert.NotNull(config);
            Assert.Equal("example.com,plugin=dns", config!.AdditionalProperties["acmedomain0"]);
            Assert.False(config.AdditionalProperties.ContainsKey("description"));
        }

        [Fact]
        public void PveNodeDns_Deserialize_HasDocumentedFields()
        {
            var json = @"{""data"": {""dns1"": ""8.8.8.8"", ""dns2"": ""8.8.4.4"", ""dns3"": ""1.1.1.1"", ""search"": ""example.com""}}";
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var dns = data.ToObject<PveNodeDns>();
            Assert.NotNull(dns);
            Assert.Equal("8.8.8.8", dns.Dns1);
            Assert.Equal("8.8.4.4", dns.Dns2);
            Assert.Equal("1.1.1.1", dns.Dns3);
            Assert.Equal("example.com", dns.Search);
        }

        [Fact]
        public void PveNodeDns_UnmappedKey_LandsInAdditionalProperties()
        {
            var json = @"{""data"": {""dns1"": ""8.8.8.8"", ""dns4"": ""9.9.9.9""}}";
            var data = JObject.Parse(json)["data"];
            var dns = data!.ToObject<PveNodeDns>();
            Assert.NotNull(dns);
            Assert.Equal("9.9.9.9", dns!.AdditionalProperties["dns4"]);
            Assert.False(dns.AdditionalProperties.ContainsKey("dns1"));
        }
    }
}
