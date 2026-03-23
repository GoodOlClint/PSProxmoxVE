using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class StorageServiceTests
    {
        private readonly Mock<IPveHttpClient> _mockClient;
        private readonly StorageService _service;
        private readonly PveSession _session;

        public StorageServiceTests()
        {
            _mockClient = new Mock<IPveHttpClient>();
            _service = new StorageService(_mockClient.Object);
            _session = new PveSession(
                "pve.example.com",
                8006,
                skipCertificateCheck: true,
                apiToken: "root@pam!test=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        // -----------------------------------------------------------------
        // GetStorages
        // -----------------------------------------------------------------

        [Fact]
        public void GetStorages_ClusterWide_ReturnsStorageArray()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("storage"))
                .ReturnsAsync(@"{""data"":[
                    {""storage"":""local"",""type"":""dir"",""content"":""images,iso,vztmpl"",""total"":107374182400,""used"":53687091200,""avail"":53687091200,""enabled"":1,""shared"":0,""active"":1},
                    {""storage"":""local-lvm"",""type"":""lvmthin"",""content"":""images,rootdir"",""total"":214748364800,""used"":107374182400,""avail"":107374182400,""enabled"":1,""shared"":0,""active"":1}
                ]}");

            // Act
            var result = _service.GetStorages(_session);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal("local", result[0].Storage);
            Assert.Equal("dir", result[0].Type);
            Assert.Equal(107374182400L, result[0].Total);
            Assert.Equal("local-lvm", result[1].Storage);
            Assert.Equal("lvmthin", result[1].Type);
            _mockClient.Verify(c => c.GetAsync("storage"), Times.Once);
        }

        [Fact]
        public void GetStorages_ByNode_QueriesNodeEndpoint()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("nodes/pve1/storage"))
                .ReturnsAsync(@"{""data"":[{""storage"":""ceph-pool"",""type"":""rbd"",""content"":""images"",""enabled"":1,""shared"":1,""active"":1}]}");

            // Act
            var result = _service.GetStorages(_session, "pve1");

            // Assert
            Assert.Single(result);
            Assert.Equal("ceph-pool", result[0].Storage);
            Assert.Equal("rbd", result[0].Type);
            _mockClient.Verify(c => c.GetAsync("nodes/pve1/storage"), Times.Once);
        }

        [Fact]
        public void GetStorages_EmptyData_ReturnsEmptyArray()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("storage"))
                .ReturnsAsync(@"{""data"":[]}");

            // Act
            var result = _service.GetStorages(_session);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetStorages_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetStorages(null!));
        }

        // -----------------------------------------------------------------
        // GetStorageContent
        // -----------------------------------------------------------------

        [Fact]
        public void GetStorageContent_ReturnsContentArray()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("nodes/pve1/storage/local/content"))
                .ReturnsAsync(@"{""data"":[
                    {""volid"":""local:iso/debian-12.iso"",""content"":""iso"",""format"":""iso"",""size"":3909091328,""ctime"":1700000000},
                    {""volid"":""local:vztmpl/ubuntu-22.04-standard.tar.zst"",""content"":""vztmpl"",""format"":""tgz"",""size"":131072000,""ctime"":1700100000}
                ]}");

            // Act
            var result = _service.GetStorageContent(_session, "pve1", "local");

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal("local:iso/debian-12.iso", result[0].VolId);
            Assert.Equal("iso", result[0].Content);
            Assert.Equal(3909091328L, result[0].Size);
            Assert.Equal("local:vztmpl/ubuntu-22.04-standard.tar.zst", result[1].VolId);
        }

        [Fact]
        public void GetStorageContent_WithContentTypeFilter_AppendsQueryParam()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("nodes/pve1/storage/local/content?content=iso"))
                .ReturnsAsync(@"{""data"":[{""volid"":""local:iso/debian-12.iso"",""content"":""iso"",""format"":""iso"",""size"":3909091328}]}");

            // Act
            var result = _service.GetStorageContent(_session, "pve1", "local", "iso");

            // Assert
            Assert.Single(result);
            Assert.Equal("iso", result[0].Content);
            _mockClient.Verify(c => c.GetAsync("nodes/pve1/storage/local/content?content=iso"), Times.Once);
        }

        [Fact]
        public void GetStorageContent_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetStorageContent(null!, "pve1", "local"));
        }

        [Fact]
        public void GetStorageContent_NullNode_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetStorageContent(_session, null!, "local"));
        }

        [Fact]
        public void GetStorageContent_NullStorage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetStorageContent(_session, "pve1", null!));
        }

        // -----------------------------------------------------------------
        // CreateStorage
        // -----------------------------------------------------------------

        [Fact]
        public void CreateStorage_ReturnsCreatedStorage()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                ["storage"] = "nfs-backup",
                ["type"] = "nfs",
                ["server"] = "192.168.1.10",
                ["export"] = "/mnt/backup",
                ["content"] = "backup"
            };

            _mockClient.Setup(c => c.PostAsync("storage", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":{""storage"":""nfs-backup"",""type"":""nfs"",""content"":""backup"",""enabled"":1,""shared"":1}}");

            // Act
            var result = _service.CreateStorage(_session, config);

            // Assert
            Assert.Equal("nfs-backup", result.Storage);
            Assert.Equal("nfs", result.Type);
            Assert.Equal("backup", result.Content);
            _mockClient.Verify(c => c.PostAsync("storage", It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Fact]
        public void CreateStorage_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateStorage(null!, new Dictionary<string, object>()));
        }

        [Fact]
        public void CreateStorage_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateStorage(_session, null!));
        }

        // -----------------------------------------------------------------
        // UpdateStorage
        // -----------------------------------------------------------------

        [Fact]
        public void UpdateStorage_CallsPutWithCorrectPath()
        {
            // Arrange
            var config = new Dictionary<string, string> { ["content"] = "backup,images" };
            _mockClient.Setup(c => c.PutAsync("storage/nfs-backup", config))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.UpdateStorage(_session, "nfs-backup", config);

            // Assert
            _mockClient.Verify(c => c.PutAsync("storage/nfs-backup", config), Times.Once);
        }

        [Fact]
        public void UpdateStorage_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UpdateStorage(null!, "local", new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateStorage_NullStorage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UpdateStorage(_session, null!, new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateStorage_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UpdateStorage(_session, "local", null!));
        }

        // -----------------------------------------------------------------
        // RemoveStorage
        // -----------------------------------------------------------------

        [Fact]
        public void RemoveStorage_CallsDeleteWithCorrectPath()
        {
            // Arrange
            _mockClient.Setup(c => c.DeleteAsync("storage/nfs-backup"))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.RemoveStorage(_session, "nfs-backup");

            // Assert
            _mockClient.Verify(c => c.DeleteAsync("storage/nfs-backup"), Times.Once);
        }

        [Fact]
        public void RemoveStorage_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveStorage(null!, "local"));
        }

        [Fact]
        public void RemoveStorage_NullStorage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveStorage(_session, null!));
        }

        // -----------------------------------------------------------------
        // GetStorageStatus
        // -----------------------------------------------------------------

        [Fact]
        public void GetStorageStatus_ReturnsStatus()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("nodes/pve1/storage/local/status"))
                .ReturnsAsync(@"{""data"":{""total"":107374182400,""used"":53687091200,""avail"":53687091200,""active"":1,""enabled"":1,""shared"":0,""type"":""dir"",""content"":""images,iso""}}");

            // Act
            var result = _service.GetStorageStatus(_session, "pve1", "local");

            // Assert
            Assert.Equal(107374182400L, result.Total);
            Assert.Equal(53687091200L, result.Used);
            Assert.Equal(53687091200L, result.Available);
            Assert.Equal(1, result.Active);
            Assert.Equal("dir", result.Type);
        }

        [Fact]
        public void GetStorageStatus_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetStorageStatus(null!, "pve1", "local"));
        }

        [Fact]
        public void GetStorageStatus_NullNode_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetStorageStatus(_session, null!, "local"));
        }

        [Fact]
        public void GetStorageStatus_NullStorage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetStorageStatus(_session, "pve1", null!));
        }

        // -----------------------------------------------------------------
        // RemoveContent
        // -----------------------------------------------------------------

        [Fact]
        public void RemoveContent_CallsDeleteWithCorrectPath()
        {
            // Arrange
            _mockClient.Setup(c => c.DeleteAsync("nodes/pve1/storage/local/content/local%3Aiso%2Fdebian-12.iso"))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.RemoveContent(_session, "pve1", "local", "local:iso/debian-12.iso");

            // Assert
            _mockClient.Verify(c => c.DeleteAsync("nodes/pve1/storage/local/content/local%3Aiso%2Fdebian-12.iso"), Times.Once);
        }

        [Fact]
        public void RemoveContent_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveContent(null!, "pve1", "local", "vol"));
        }

        [Fact]
        public void RemoveContent_NullVolume_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveContent(_session, "pve1", "local", null!));
        }

        // -----------------------------------------------------------------
        // UpdateContent
        // -----------------------------------------------------------------

        [Fact]
        public void UpdateContent_CallsPutWithCorrectPath()
        {
            // Arrange
            var config = new Dictionary<string, string> { ["notes"] = "Weekly backup" };
            _mockClient.Setup(c => c.PutAsync(
                    "nodes/pve1/storage/local/content/local%3Abackup%2Fvzdump-qemu-100.vma.zst",
                    config))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.UpdateContent(_session, "pve1", "local", "local:backup/vzdump-qemu-100.vma.zst", config);

            // Assert
            _mockClient.Verify(c => c.PutAsync(
                "nodes/pve1/storage/local/content/local%3Abackup%2Fvzdump-qemu-100.vma.zst",
                config), Times.Once);
        }

        [Fact]
        public void UpdateContent_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UpdateContent(null!, "pve1", "local", "vol", new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateContent_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UpdateContent(_session, "pve1", "local", "vol", null!));
        }

        // -----------------------------------------------------------------
        // UploadIso
        // -----------------------------------------------------------------

        [Fact]
        public void UploadIso_ReturnsTaskWithUpid()
        {
            // Arrange
            _mockClient.Setup(c => c.UploadFileAsync(
                    "nodes/pve1/storage/local/upload",
                    "/tmp/debian-12.iso",
                    It.IsAny<Dictionary<string, string>>(),
                    null, null, null))
                .ReturnsAsync(@"{""data"":""UPID:pve1:000AAA:00000001:65F00000:upload:local:root@pam:""}");

            // Act
            var result = _service.UploadIso(_session, "pve1", "local", "/tmp/debian-12.iso");

            // Assert
            Assert.Contains("UPID:pve1", result.Upid);
            Assert.Equal("pve1", result.Node);
        }

        [Fact]
        public void UploadIso_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UploadIso(null!, "pve1", "local", "/tmp/test.iso"));
        }

        [Fact]
        public void UploadIso_NullFilePath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.UploadIso(_session, "pve1", "local", null!));
        }

        // -----------------------------------------------------------------
        // DownloadUrl
        // -----------------------------------------------------------------

        [Fact]
        public void DownloadUrl_ReturnsTaskWithUpid()
        {
            // Arrange
            _mockClient.Setup(c => c.PostAsync(
                    "nodes/pve1/storage/local/download-url",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":""UPID:pve1:000BBB:00000002:65F00001:download:local:root@pam:""}");

            // Act
            var result = _service.DownloadUrl(
                _session, "pve1", "local",
                "https://cloud-images.ubuntu.com/noble/current/noble-server-cloudimg-amd64.img",
                "noble-server-cloudimg-amd64.img", "iso");

            // Assert
            Assert.Contains("UPID:pve1", result.Upid);
            Assert.Equal("pve1", result.Node);
        }

        [Fact]
        public void DownloadUrl_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.DownloadUrl(null!, "pve1", "local", "https://example.com/f.iso", "f.iso", "iso"));
        }

        [Fact]
        public void DownloadUrl_NullUrl_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.DownloadUrl(_session, "pve1", "local", null!, "f.iso", "iso"));
        }

        [Fact]
        public void DownloadUrl_NullFilename_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.DownloadUrl(_session, "pve1", "local", "https://example.com/f.iso", null!, "iso"));
        }

        [Fact]
        public void DownloadUrl_NullContentType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.DownloadUrl(_session, "pve1", "local", "https://example.com/f.iso", "f.iso", null!));
        }

        // -----------------------------------------------------------------
        // AllocateDisk
        // -----------------------------------------------------------------

        [Fact]
        public void AllocateDisk_ReturnsTaskWithUpid()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["filename"] = "vm-200-disk-0",
                ["size"] = "32G",
                ["format"] = "qcow2"
            };
            _mockClient.Setup(c => c.PostAsync(
                    "nodes/pve1/storage/local-lvm/content",
                    config))
                .ReturnsAsync(@"{""data"":""UPID:pve1:000CCC:00000003:65F00002:alloc:local-lvm:root@pam:""}");

            // Act
            var result = _service.AllocateDisk(_session, "pve1", "local-lvm", config);

            // Assert
            Assert.Contains("UPID:pve1", result.Upid);
            Assert.Equal("pve1", result.Node);
        }

        [Fact]
        public void AllocateDisk_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.AllocateDisk(null!, "pve1", "local", new Dictionary<string, string>()));
        }

        [Fact]
        public void AllocateDisk_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.AllocateDisk(_session, "pve1", "local", null!));
        }
    }
}
