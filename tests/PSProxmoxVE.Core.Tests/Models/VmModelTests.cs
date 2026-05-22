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

        [Fact]
        public void PveVmConfig_Deserialize_SurfacesScsiHardware()
        {
            var json = TestHelper.LoadFixture("pve9_vm_config.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var config = data.ToObject<PveVmConfig>();
            Assert.NotNull(config);
            Assert.Equal("virtio-scsi-single", config.ScsiHardware);
        }

        [Fact]
        public void PveVmConfig_Deserialize_SurfacesEfiDiskAndTpm()
        {
            var json = @"{ ""efidisk0"": ""local-lvm:vm-100-disk-1,efitype=4m,size=528K"",
                           ""tpmstate0"": ""local-lvm:vm-100-disk-2,size=4M,version=v2.0"" }";
            var config = JObject.Parse(json).ToObject<PveVmConfig>();
            Assert.NotNull(config);
            Assert.Contains("efitype=4m", config!.EfiDisk0);
            Assert.Contains("version=v2.0", config.TpmState0);
        }

        [Fact]
        public void PveVmConfig_UnmappedKeys_LandInAdditionalProperties_AsNativeTypes()
        {
            // hostpci0 and numa0 are not typed properties; they must not be dropped.
            var json = @"{ ""cores"": 2,
                           ""hostpci0"": ""0000:01:00.0,pcie=1"",
                           ""numa0"": ""cpus=0-1,memory=2048"" }";
            var config = JObject.Parse(json).ToObject<PveVmConfig>();
            Assert.NotNull(config);

            Assert.Equal(2, config!.Cores); // typed property still works
            Assert.True(config.AdditionalProperties.ContainsKey("hostpci0"));
            Assert.Equal("0000:01:00.0,pcie=1", config.AdditionalProperties["hostpci0"]);
            // Value must be a native type (string), never a Newtonsoft JToken (D013).
            Assert.IsType<string>(config.AdditionalProperties["hostpci0"]);
            Assert.DoesNotContain("Newtonsoft", config.AdditionalProperties["hostpci0"]!.GetType().FullName);
        }

        [Fact]
        public void PveVmConfig_TypedKeys_DoNotLeakIntoAdditionalProperties()
        {
            var json = TestHelper.LoadFixture("pve9_vm_config.json");
            var data = JObject.Parse(json)["data"];
            var config = data!.ToObject<PveVmConfig>();
            Assert.NotNull(config);
            // scsihw and cores are typed → they must not also appear in the catch-all.
            Assert.False(config!.AdditionalProperties.ContainsKey("scsihw"));
            Assert.False(config.AdditionalProperties.ContainsKey("cores"));
        }
    }
}
