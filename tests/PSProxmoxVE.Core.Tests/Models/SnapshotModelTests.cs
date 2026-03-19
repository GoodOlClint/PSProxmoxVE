using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class SnapshotModelTests
    {
        [Fact]
        public void PveSnapshot_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_snapshots.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var snapshots = data.ToObject<PveSnapshot[]>();
            Assert.NotNull(snapshots);
            Assert.Equal(3, snapshots.Length);
        }

        [Fact]
        public void PveSnapshot_Deserialize_Pve9_Current_HasCorrectName()
        {
            var json = TestHelper.LoadFixture("pve9_snapshots.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var snapshots = data.ToObject<PveSnapshot[]>();
            Assert.NotNull(snapshots);
            Assert.Equal("current", snapshots[0].Name);
        }

        [Fact]
        public void PveSnapshot_Deserialize_Pve9_Current_HasDescription()
        {
            var json = TestHelper.LoadFixture("pve9_snapshots.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var snapshots = data.ToObject<PveSnapshot[]>();
            Assert.NotNull(snapshots);
            Assert.Equal("You are here!", snapshots[0].Description);
        }

        [Fact]
        public void PveSnapshot_Deserialize_Pve9_Snap1_HasSnapTime()
        {
            var json = TestHelper.LoadFixture("pve9_snapshots.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var snapshots = data.ToObject<PveSnapshot[]>();
            Assert.NotNull(snapshots);
            Assert.Equal("snap1", snapshots[1].Name);
            Assert.Equal(1710000000L, snapshots[1].SnapTime);
        }

        [Fact]
        public void PveSnapshot_Deserialize_Pve9_Snap1_HasVmState()
        {
            var json = TestHelper.LoadFixture("pve9_snapshots.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var snapshots = data.ToObject<PveSnapshot[]>();
            Assert.NotNull(snapshots);
            Assert.Equal(1, snapshots[1].VmState);
        }

        [Fact]
        public void PveSnapshot_Deserialize_Pve9_Snap1_HasParent()
        {
            var json = TestHelper.LoadFixture("pve9_snapshots.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var snapshots = data.ToObject<PveSnapshot[]>();
            Assert.NotNull(snapshots);
            Assert.Equal("base", snapshots[1].Parent);
        }

        [Fact]
        public void PveSnapshot_Deserialize_Pve9_Base_HasNoVmState()
        {
            var json = TestHelper.LoadFixture("pve9_snapshots.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var snapshots = data.ToObject<PveSnapshot[]>();
            Assert.NotNull(snapshots);
            Assert.Equal("base", snapshots[2].Name);
            Assert.Equal(0, snapshots[2].VmState);
        }

        [Fact]
        public void PveSnapshot_Deserialize_Pve9_Current_ParentIsSnap1()
        {
            var json = TestHelper.LoadFixture("pve9_snapshots.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var snapshots = data.ToObject<PveSnapshot[]>();
            Assert.NotNull(snapshots);
            Assert.Equal("snap1", snapshots[0].Parent);
        }

        [Fact]
        public void PveSnapshot_Deserialize_Pve9_Base_HasOlderSnapTime()
        {
            var json = TestHelper.LoadFixture("pve9_snapshots.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var snapshots = data.ToObject<PveSnapshot[]>();
            Assert.NotNull(snapshots);
            Assert.Equal(1709000000L, snapshots[2].SnapTime);
        }
    }
}
