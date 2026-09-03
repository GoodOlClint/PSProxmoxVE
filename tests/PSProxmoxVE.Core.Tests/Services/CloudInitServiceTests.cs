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
    public class CloudInitServiceTests
    {
        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        [Fact]
        public void SetCloudInitConfig_CallsPutAsyncWithCorrectResource()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PutAsync(
                    "nodes/pve1/qemu/100/config",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");
            var service = new CloudInitService(mockClient.Object);
            var config = new Dictionary<string, object>
            {
                ["ciuser"] = "ubuntu",
                ["ipconfig0"] = "ip=dhcp"
            };

            // Act
            service.SetCloudInitConfig(CreateSession(), "pve1", 100, config);

            // Assert
            mockClient.Verify(c => c.PutAsync(
                "nodes/pve1/qemu/100/config",
                It.Is<Dictionary<string, string>>(d =>
                    d["ciuser"] == "ubuntu" && d["ipconfig0"] == "ip=dhcp")),
                Times.Once);
        }

        [Fact]
        public void RegenerateCloudInitImage_CallsPutAsyncAndReturnsUpid()
        {
            // Arrange
            var json = @"{""data"": ""UPID:pve1:00001234:00005678:12345678:cloudinit:100:root@pam:""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PutAsync("nodes/pve1/qemu/100/cloudinit", null))
                .ReturnsAsync(json);
            var service = new CloudInitService(mockClient.Object);

            // Act
            var result = service.RegenerateCloudInitImage(CreateSession(), "pve1", 100);

            // Assert
            Assert.Contains("UPID:", result);
            mockClient.Verify(c => c.PutAsync("nodes/pve1/qemu/100/cloudinit", null), Times.Once);
        }

        [Fact]
        public void Constructor_NullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CloudInitService(null!));
        }

        [Fact]
        public void GetFullVmConfig_ReturnsFullConfig_IncludingCloudInitAndOtherFields()
        {
            // Arrange
            var json = @"{""data"": {
                ""ciuser"": ""ubuntu"",
                ""ipconfig0"": ""ip=dhcp"",
                ""cores"": 4,
                ""memory"": 8192
            }}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes/pve1/qemu/100/config")).ReturnsAsync(json);
            var service = new CloudInitService(mockClient.Object);

            // Act
            var config = service.GetFullVmConfig(CreateSession(), "pve1", 100);

            // Assert — cloud-init fields and every other config field are both present
            Assert.Equal("ubuntu", config.CiUser);
            Assert.Equal("ip=dhcp", config.IpConfig0);
            Assert.Equal(4, config.Cores);
            Assert.Equal(8192, config.Memory);
            mockClient.Verify(c => c.GetAsync("nodes/pve1/qemu/100/config"), Times.Once);
        }

        [Fact]
        public void GetFullVmConfig_EscapesNodeInPath()
        {
            // Arrange
            var json = @"{""data"": {}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes/pve%20node/qemu/100/config")).ReturnsAsync(json);
            var service = new CloudInitService(mockClient.Object);

            // Act
            service.GetFullVmConfig(CreateSession(), "pve node", 100);

            // Assert
            mockClient.Verify(c => c.GetAsync("nodes/pve%20node/qemu/100/config"), Times.Once);
        }
    }
}
