using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Containers;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class ContainerModelTests
    {
        [Fact]
        public void PveContainer_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal(2, containers.Length);
        }

        [Fact]
        public void PveContainer_Deserialize_Pve9_FirstContainer_HasCorrectVmId()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal(200, containers[0].VmId);
        }

        [Fact]
        public void PveContainer_Deserialize_Pve9_FirstContainer_HasCorrectName()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal("ct-web", containers[0].Name);
        }

        [Fact]
        public void PveContainer_Deserialize_Pve9_FirstContainer_HasCorrectStatus()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal("running", containers[0].Status);
        }

        [Fact]
        public void PveContainer_Deserialize_Pve9_FirstContainer_HasCorrectNode()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal("pve1", containers[0].Node);
        }

        [Fact]
        public void PveContainer_Deserialize_Pve9_FirstContainer_HasCorrectCpuAndMem()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal(2, containers[0].CpuCount);
            Assert.Equal(2147483648L, containers[0].MaxMem);
        }

        [Fact]
        public void PveContainer_Deserialize_Pve9_FirstContainer_HasSwapAndDisk()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal(536870912L, containers[0].MaxSwap);
            Assert.Equal(8589934592L, containers[0].RootFsSize);
        }

        [Fact]
        public void PveContainer_Deserialize_Pve9_FirstContainer_IsUnprivileged()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal(1, containers[0].Unprivileged);
        }

        [Fact]
        public void PveContainer_Deserialize_Pve9_FirstContainer_HasOsType()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal("ubuntu", containers[0].OsType);
        }

        [Fact]
        public void PveContainer_Deserialize_Pve9_SecondContainer_IsStopped()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            Assert.Equal(201, containers[1].VmId);
            Assert.Equal("stopped", containers[1].Status);
            Assert.Equal(0L, containers[1].Uptime);
        }

        [Fact]
        public void PveContainer_OptionalFields_NullWhenMissing()
        {
            var json = TestHelper.LoadFixture("pve9_containers.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var containers = data.ToObject<PveContainer[]>();
            Assert.NotNull(containers);
            // Tags field is only in the first container
            Assert.Equal("web", containers[0].Tags);
            Assert.Null(containers[1].Tags);
        }
    }
}
