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
            var status = data.ToObject<PveNodeStatus>();
            Assert.NotNull(status);
            Assert.Equal(0.125, status.CpuUsage);
        }

        [Fact]
        public void PveNodeStatus_Deserialize_Pve9_HasMemoryAndUptime()
        {
            var json = TestHelper.LoadFixture("pve9_node_status.json");
            var data = JObject.Parse(json)["data"];
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
            var nodes = data.ToObject<PveNode[]>();
            Assert.NotNull(nodes);
            Assert.NotNull(nodes[0].LoadAverage);
            Assert.Equal(3, nodes[0].LoadAverage!.Length);
        }
    }
}
