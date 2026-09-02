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
    }
}
