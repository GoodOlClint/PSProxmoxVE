using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Storage;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class StorageModelTests
    {
        [Fact]
        public void PveStorage_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_storage.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var storages = data.ToObject<PveStorage[]>();
            Assert.NotNull(storages);
            Assert.Equal(3, storages.Length);
        }

        [Fact]
        public void PveStorage_Deserialize_Pve9_Local_HasCorrectId()
        {
            var json = TestHelper.LoadFixture("pve9_storage.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var storages = data.ToObject<PveStorage[]>();
            Assert.NotNull(storages);
            Assert.Equal("local", storages[0].Storage);
        }

        [Fact]
        public void PveStorage_Deserialize_Pve9_Local_HasCorrectType()
        {
            var json = TestHelper.LoadFixture("pve9_storage.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var storages = data.ToObject<PveStorage[]>();
            Assert.NotNull(storages);
            Assert.Equal("dir", storages[0].Type);
        }

        [Fact]
        public void PveStorage_Deserialize_Pve9_Local_HasContentTypes()
        {
            var json = TestHelper.LoadFixture("pve9_storage.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var storages = data.ToObject<PveStorage[]>();
            Assert.NotNull(storages);
            Assert.Equal("iso,vztmpl,backup", storages[0].Content);
        }

        [Fact]
        public void PveStorage_Deserialize_Pve9_Local_HasSizeFields()
        {
            var json = TestHelper.LoadFixture("pve9_storage.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var storages = data.ToObject<PveStorage[]>();
            Assert.NotNull(storages);
            Assert.Equal(107374182400L, storages[0].Total);
            Assert.Equal(21474836480L, storages[0].Used);
            Assert.Equal(85899345920L, storages[0].Available);
        }

        [Fact]
        public void PveStorage_Deserialize_Pve9_Local_IsEnabled_NotShared()
        {
            var json = TestHelper.LoadFixture("pve9_storage.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var storages = data.ToObject<PveStorage[]>();
            Assert.NotNull(storages);
            Assert.Equal(1, storages[0].Enabled);
            Assert.Equal(0, storages[0].Shared);
        }

        [Fact]
        public void PveStorage_Deserialize_Pve9_CephPool_IsShared()
        {
            var json = TestHelper.LoadFixture("pve9_storage.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var storages = data.ToObject<PveStorage[]>();
            Assert.NotNull(storages);
            var ceph = storages[2];
            Assert.Equal("ceph-pool", ceph.Storage);
            Assert.Equal("rbd", ceph.Type);
            Assert.Equal(1, ceph.Shared);
        }

        [Fact]
        public void PveStorageContent_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_storage_content.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var items = data.ToObject<PveStorageContent[]>();
            Assert.NotNull(items);
            Assert.Equal(2, items.Length);
        }

        [Fact]
        public void PveStorageContent_Deserialize_Pve9_FirstItem_HasCorrectVolId()
        {
            var json = TestHelper.LoadFixture("pve9_storage_content.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var items = data.ToObject<PveStorageContent[]>();
            Assert.NotNull(items);
            Assert.Equal("local:iso/ubuntu-24.04-live-server-amd64.iso", items[0].VolId);
        }

        [Fact]
        public void PveStorageContent_Deserialize_Pve9_FirstItem_HasCorrectContentType()
        {
            var json = TestHelper.LoadFixture("pve9_storage_content.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var items = data.ToObject<PveStorageContent[]>();
            Assert.NotNull(items);
            Assert.Equal("iso", items[0].Content);
            Assert.Equal("iso", items[0].Format);
        }

        [Fact]
        public void PveStorageContent_Deserialize_Pve9_FirstItem_HasSize()
        {
            var json = TestHelper.LoadFixture("pve9_storage_content.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var items = data.ToObject<PveStorageContent[]>();
            Assert.NotNull(items);
            Assert.Equal(2774532096L, items[0].Size);
        }

        [Fact]
        public void PveStorageContent_Deserialize_Pve9_OptionalFields_NullWhenMissing()
        {
            var json = TestHelper.LoadFixture("pve9_storage_content.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var items = data.ToObject<PveStorageContent[]>();
            Assert.NotNull(items);
            // ISO items should not have a VmId or Notes
            Assert.Null(items[0].VmId);
            Assert.Null(items[0].Notes);
        }
    }
}
