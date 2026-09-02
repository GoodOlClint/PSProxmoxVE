using Moq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class NetworkServiceTests
    {
        private readonly Mock<IPveHttpClient> _mockClient;
        private readonly NetworkService _service;
        private readonly PveSession _session;

        public NetworkServiceTests()
        {
            _mockClient = new Mock<IPveHttpClient>();
            _service = new NetworkService(_mockClient.Object);
            _session = new PveSession(
                "pve.example.com",
                8006,
                skipCertificateCheck: true,
                apiToken: "root@pam!test=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        // -----------------------------------------------------------------
        // RemoveSdnZone
        // -----------------------------------------------------------------

        [Fact]
        public void RemoveSdnZone_EscapesPathTraversalInName()
        {
            // Arrange
            _mockClient.Setup(c => c.DeleteAsync("cluster/sdn/zones/..%2Faccess%2Fusers%2Fx"))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.RemoveSdnZone(_session, "../access/users/x");

            // Assert
            _mockClient.Verify(c => c.DeleteAsync("cluster/sdn/zones/..%2Faccess%2Fusers%2Fx"), Times.Once);
        }

        // -----------------------------------------------------------------
        // RemoveSdnVnet
        // -----------------------------------------------------------------

        [Fact]
        public void RemoveSdnVnet_EscapesPathTraversalInName()
        {
            // Arrange
            _mockClient.Setup(c => c.DeleteAsync("cluster/sdn/vnets/..%2Faccess%2Fusers%2Fx"))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.RemoveSdnVnet(_session, "../access/users/x");

            // Assert
            _mockClient.Verify(c => c.DeleteAsync("cluster/sdn/vnets/..%2Faccess%2Fusers%2Fx"), Times.Once);
        }
    }
}
