using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class VmModelTests
    {
        [Fact]
        public void PveVm_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_vms.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vms = data.ToObject<PveVm[]>();
            Assert.NotNull(vms);
            Assert.Equal(3, vms.Length);
        }

        [Fact]
        public void PveVm_Deserialize_Pve9_FirstVm_HasCorrectVmId()
        {
            var json = TestHelper.LoadFixture("pve9_vms.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vms = data.ToObject<PveVm[]>();
            Assert.NotNull(vms);
            Assert.Equal(100, vms[0].VmId);
        }

        [Fact]
        public void PveVm_Deserialize_Pve9_FirstVm_HasCorrectName()
        {
            var json = TestHelper.LoadFixture("pve9_vms.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vms = data.ToObject<PveVm[]>();
            Assert.NotNull(vms);
            Assert.Equal("test-vm-1", vms[0].Name);
        }

        [Fact]
        public void PveVm_Deserialize_Pve9_FirstVm_HasCorrectStatus()
        {
            var json = TestHelper.LoadFixture("pve9_vms.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vms = data.ToObject<PveVm[]>();
            Assert.NotNull(vms);
            Assert.Equal("running", vms[0].Status);
        }

        [Fact]
        public void PveVm_Deserialize_Pve9_TemplateFlag_IsSet()
        {
            var json = TestHelper.LoadFixture("pve9_vms.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vms = data.ToObject<PveVm[]>();
            Assert.NotNull(vms);
            // Third entry is the template
            var template = vms[2];
            Assert.Equal(9000, template.VmId);
            Assert.Equal(1, template.Template);
        }

        [Fact]
        public void PveVm_Deserialize_Pve9_RegularVm_TemplateFlag_IsZero()
        {
            var json = TestHelper.LoadFixture("pve9_vms.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vms = data.ToObject<PveVm[]>();
            Assert.NotNull(vms);
            Assert.Equal(0, vms[0].Template);
        }

        [Fact]
        public void PveVm_Deserialize_Pve9_FirstVm_HasExpectedMemoryAndDisk()
        {
            var json = TestHelper.LoadFixture("pve9_vms.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vms = data.ToObject<PveVm[]>();
            Assert.NotNull(vms);
            Assert.Equal(8589934592L, vms[0].MaxMem);
            Assert.Equal(107374182400L, vms[0].MaxDisk);
        }

        [Fact]
        public void PveVm_Deserialize_Pve9_StoppedVm_UptimeIsZero()
        {
            var json = TestHelper.LoadFixture("pve9_vms.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var vms = data.ToObject<PveVm[]>();
            Assert.NotNull(vms);
            Assert.Equal(0L, vms[1].Uptime);
        }

        [Fact]
        public void PveVmConfig_Deserialize_Pve9_HasCorrectCoresAndMemory()
        {
            var json = TestHelper.LoadFixture("pve9_vm_config.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var config = data.ToObject<PveVmConfig>();
            Assert.NotNull(config);
            Assert.Equal(4, config.Cores);
            Assert.Equal(1, config.Sockets);
            Assert.Equal(8192, config.Memory);
        }

        [Fact]
        public void PveVmConfig_Deserialize_Pve9_HasCorrectBiosAndMachine()
        {
            var json = TestHelper.LoadFixture("pve9_vm_config.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var config = data.ToObject<PveVmConfig>();
            Assert.NotNull(config);
            Assert.Equal("ovmf", config.Bios);
            Assert.Equal("q35", config.Machine);
        }

        [Fact]
        public void PveVmConfig_Deserialize_Pve9_HasCpuTypeAndOsType()
        {
            var json = TestHelper.LoadFixture("pve9_vm_config.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var config = data.ToObject<PveVmConfig>();
            Assert.NotNull(config);
            Assert.Equal("host", config.CpuType);
            Assert.Equal("l26", config.OsType);
        }

        [Fact]
        public void PveVmConfig_Deserialize_Pve9_HasNetworkInterface()
        {
            var json = TestHelper.LoadFixture("pve9_vm_config.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var config = data.ToObject<PveVmConfig>();
            Assert.NotNull(config);
            Assert.NotNull(config.Net0);
            Assert.Contains("vmbr0", config.Net0);
        }

        [Fact]
        public void PveVmConfig_Deserialize_Pve9_OptionalFields_NullWhenMissing()
        {
            var json = TestHelper.LoadFixture("pve9_vm_config.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var config = data.ToObject<PveVmConfig>();
            Assert.NotNull(config);
            // Fields not present in fixture should be null
            Assert.Null(config.Net1);
            Assert.Null(config.Virtio1);
            Assert.Null(config.Args);
        }

        [Fact]
        public void PveVmConfig_Deserialize_Pve9_CloudInitFields()
        {
            var json = TestHelper.LoadFixture("pve9_vm_config.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var config = data.ToObject<PveVmConfig>();
            Assert.NotNull(config);
            Assert.Equal("admin", config.CiUser);
            Assert.Equal("8.8.8.8", config.Nameserver);
            Assert.Equal("example.com", config.Searchdomain);
        }
    }
}
