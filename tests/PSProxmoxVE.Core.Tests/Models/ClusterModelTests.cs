using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Cluster;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class ClusterModelTests
    {
        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            Assert.Equal(4, entries.Length);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_FirstEntry_IsClusterType()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            Assert.Equal("cluster", entries[0].Type);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_ClusterEntry_HasCorrectName()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            Assert.Equal("testcluster", entries[0].Name);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_ClusterEntry_HasNodeCount()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            Assert.Equal(3, entries[0].Nodes);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_ClusterEntry_IsQuorate()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            Assert.Equal(1, entries[0].Quorate);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_SecondEntry_IsNodeType()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            Assert.Equal("node", entries[1].Type);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_FirstNodeEntry_HasCorrectIp()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            Assert.Equal("192.168.1.10", entries[1].Ip);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_FirstNodeEntry_IsOnline()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            Assert.Equal(1, entries[1].Online);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_FirstNodeEntry_IsLocal()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            Assert.Equal(1, entries[1].Local);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_ThirdNode_IsOffline()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            // Fourth entry (index 3) is pve3 which is offline
            Assert.Equal("node", entries[3].Type);
            Assert.Equal(0, entries[3].Online);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_NodeFields_NullForClusterEntry()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            // Cluster-type entry should not have node-specific fields
            Assert.Null(entries[0].Ip);
            Assert.Null(entries[0].Online);
        }

        [Fact]
        public void PveClusterStatus_Deserialize_Pve9_ClusterFields_NullForNodeEntry()
        {
            var json = TestHelper.LoadFixture("pve9_cluster_status.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveClusterStatus[]>();
            Assert.NotNull(entries);
            // Node-type entries should not have cluster-specific fields
            Assert.Null(entries[1].Nodes);
            Assert.Null(entries[1].Quorate);
        }

        [Fact]
        public void PveClusterConfigEntry_Deserialize_HasNameFromLinksTemplate()
        {
            // GET /cluster/config is a directory index: the item schema documents
            // no named fields, but the endpoint's "links" metadata gives the
            // child-URL template as "{name}", so every entry carries a "name" key.
            var json = @"{""data"": [{""name"": ""nodes""}, {""name"": ""totem""}]}";
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<System.Collections.Generic.List<PveClusterConfigEntry>>();
            Assert.NotNull(entries);
            Assert.Equal(2, entries!.Count);
            Assert.Equal("nodes", entries[0].Name);
            Assert.Equal("totem", entries[1].Name);
        }

        [Fact]
        public void PveClusterConfigEntry_UnmappedKey_LandsInAdditionalProperties()
        {
            var json = @"{""data"": [{""name"": ""nodes"", ""extra"": ""x""}]}";
            var data = JObject.Parse(json)["data"];
            var entries = data!.ToObject<System.Collections.Generic.List<PveClusterConfigEntry>>();
            Assert.NotNull(entries);
            Assert.Equal("x", entries![0].AdditionalProperties["extra"]);
            Assert.False(entries[0].AdditionalProperties.ContainsKey("name"));
        }
    }
}
