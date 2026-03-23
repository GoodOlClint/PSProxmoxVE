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
        public void GetCloudInitConfig_ReturnsConfig()
        {
            // Arrange
            var json = @"{""data"": {
                ""ciuser"": ""ubuntu"",
                ""ipconfig0"": ""ip=dhcp"",
                ""nameserver"": ""8.8.8.8"",
                ""searchdomain"": ""example.com"",
                ""sshkeys"": ""ssh-rsa%20AAAA...%20user%40host"",
                ""boot"": ""order=scsi0;net0"",
                ""cores"": 4,
                ""memory"": 8192
            }}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes/pve1/qemu/100/config")).ReturnsAsync(json);
            var service = new CloudInitService(mockClient.Object);

            // Act
            var config = service.GetCloudInitConfig(CreateSession(), "pve1", 100);

            // Assert
            Assert.NotNull(config);
            Assert.Equal("ubuntu", config.CiUser);
            Assert.Equal("ip=dhcp", config.IpConfig0);
            Assert.Equal("8.8.8.8", config.Nameserver);
            Assert.Equal("example.com", config.Searchdomain);
            Assert.Equal("ssh-rsa%20AAAA...%20user%40host", config.SshKeys);
            mockClient.Verify(c => c.GetAsync("nodes/pve1/qemu/100/config"), Times.Once);
        }

        [Fact]
        public void GetCloudInitConfig_ExcludesNonCiFields()
        {
            // Arrange — response includes non-CI fields that should be ignored
            var json = @"{""data"": {
                ""ciuser"": ""admin"",
                ""cores"": 4,
                ""memory"": 8192,
                ""boot"": ""order=scsi0""
            }}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes/pve1/qemu/200/config")).ReturnsAsync(json);
            var service = new CloudInitService(mockClient.Object);

            // Act
            var config = service.GetCloudInitConfig(CreateSession(), "pve1", 200);

            // Assert
            Assert.Equal("admin", config.CiUser);
            // Non-CI fields should not appear in the result
            Assert.Null(config.IpConfig0);
            Assert.Null(config.Nameserver);
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
        public void RegenerateCloudInitImage_CallsGetAsyncAndReturnsData()
        {
            // Arrange
            var json = @"{""data"": ""#cloud-config\nuser: ubuntu\npassword: secret\n""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes/pve1/qemu/100/cloudinit/dump?type=user"))
                .ReturnsAsync(json);
            var service = new CloudInitService(mockClient.Object);

            // Act
            var result = service.RegenerateCloudInitImage(CreateSession(), "pve1", 100);

            // Assert
            Assert.Contains("#cloud-config", result);
            Assert.Contains("ubuntu", result);
            mockClient.Verify(c => c.GetAsync("nodes/pve1/qemu/100/cloudinit/dump?type=user"), Times.Once);
        }

        [Fact]
        public void GetCloudInitConfig_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new CloudInitService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetCloudInitConfig(null!, "pve1", 100));
        }

        [Fact]
        public void SetCloudInitConfig_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new CloudInitService(mockClient.Object);
            var config = new Dictionary<string, object> { ["ciuser"] = "test" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.SetCloudInitConfig(null!, "pve1", 100, config));
        }

        [Fact]
        public void RegenerateCloudInitImage_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new CloudInitService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.RegenerateCloudInitImage(null!, "pve1", 100));
        }

        [Fact]
        public void Constructor_NullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CloudInitService(null!));
        }
    }
}
