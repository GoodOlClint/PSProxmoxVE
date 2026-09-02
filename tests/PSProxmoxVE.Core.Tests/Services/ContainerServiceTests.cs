using System.Collections.Generic;
using Moq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Services;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class ContainerServiceTests
    {
        private const string TestNode = "pve1";
        private const int TestVmId = 100;

        private static PveSession CreateSession() =>
            new PveSession("pve1.example.com", 8006, true, "PVE:root@pam:TEST_TOKEN");

        [Fact]
        public void RemoveContainer_WithForceTrue_IncludesForceInQueryString()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .Callback<string>(r => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzdestroy:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.RemoveContainer(CreateSession(), TestNode, TestVmId, purge: false, force: true);

            Assert.Equal($"nodes/{TestNode}/lxc/{TestVmId}?purge=0&force=1", resource);
        }

        [Fact]
        public void RemoveContainer_WithForceFalse_OmitsForceFromQueryString()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .Callback<string>(r => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzdestroy:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.RemoveContainer(CreateSession(), TestNode, TestVmId, purge: false, force: false);

            Assert.Equal($"nodes/{TestNode}/lxc/{TestVmId}?purge=0", resource);
        }

        [Fact]
        public void RemoveContainer_WithPurgeAndForce_IncludesBothInQueryString()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .Callback<string>(r => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzdestroy:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.RemoveContainer(CreateSession(), TestNode, TestVmId, purge: true, force: true);

            Assert.Equal($"nodes/{TestNode}/lxc/{TestVmId}?purge=1&force=1", resource);
        }

        [Fact]
        public void CloneContainer_WithStorage_IncludesStorageInFormBody()
        {
            Dictionary<string, string>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzclone:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.CloneContainer(CreateSession(), TestNode, TestVmId, 200, storage: "local-zfs");

            Assert.NotNull(captured);
            Assert.Equal("local-zfs", captured!["storage"]);
        }

        [Fact]
        public void CloneContainer_WithoutStorage_OmitsStorageFromFormBody()
        {
            Dictionary<string, string>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzclone:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.CloneContainer(CreateSession(), TestNode, TestVmId, 200);

            Assert.NotNull(captured);
            Assert.False(captured!.ContainsKey("storage"));
        }

        [Fact]
        public void CloneContainer_SendsAllocatedNewidNeverZero()
        {
            Dictionary<string, string>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzclone:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.CloneContainer(CreateSession(), TestNode, TestVmId, 305);

            Assert.NotNull(captured);
            Assert.Equal("305", captured!["newid"]);
        }
    }
}
