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
    public class ClusterServiceTests
    {
        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        [Fact]
        public void GetClusterStatus_ReturnsStatusArray()
        {
            // Arrange
            var json = @"{""data"": [
                {""type"": ""cluster"", ""name"": ""pve-cluster"", ""nodes"": 3, ""quorate"": 1, ""version"": 5},
                {""type"": ""node"", ""name"": ""pve1"", ""online"": 1, ""local"": 1, ""nodeid"": 1, ""ip"": ""10.0.0.1""},
                {""type"": ""node"", ""name"": ""pve2"", ""online"": 1, ""local"": 0, ""nodeid"": 2, ""ip"": ""10.0.0.2""}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/status")).ReturnsAsync(json);
            var service = new ClusterService(mockClient.Object);

            // Act
            var statuses = service.GetClusterStatus(CreateSession());

            // Assert
            Assert.Equal(3, statuses.Length);
            Assert.Equal("cluster", statuses[0].Type);
            Assert.Equal("pve-cluster", statuses[0].Name);
            Assert.Equal(3, statuses[0].Nodes);
            Assert.Equal(1, statuses[0].Quorate);
            Assert.Equal("node", statuses[1].Type);
            Assert.Equal("pve1", statuses[1].Name);
            Assert.Equal(1, statuses[1].Online);
            Assert.Equal("10.0.0.1", statuses[1].Ip);
            Assert.Equal("node", statuses[2].Type);
            Assert.Equal("pve2", statuses[2].Name);
            mockClient.Verify(c => c.GetAsync("cluster/status"), Times.Once);
        }

        [Fact]
        public void GetClusterResources_ReturnsResourcesArray()
        {
            // Arrange
            var json = @"{""data"": [
                {""id"": ""node/pve1"", ""type"": ""node"", ""node"": ""pve1"", ""status"": ""online"", ""maxcpu"": 16, ""maxmem"": 68719476736, ""cpu"": 0.05},
                {""id"": ""qemu/100"", ""type"": ""qemu"", ""node"": ""pve1"", ""name"": ""test-vm"", ""status"": ""running"", ""vmid"": 100, ""maxcpu"": 4, ""maxmem"": 8589934592},
                {""id"": ""storage/local"", ""type"": ""storage"", ""node"": ""pve1"", ""status"": ""available"", ""maxdisk"": 107374182400, ""disk"": 53687091200}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/resources")).ReturnsAsync(json);
            var service = new ClusterService(mockClient.Object);

            // Act
            var resources = service.GetClusterResources(CreateSession());

            // Assert
            Assert.Equal(3, resources.Length);
            Assert.Equal("node/pve1", resources[0].Id);
            Assert.Equal("node", resources[0].Type);
            Assert.Equal("qemu/100", resources[1].Id);
            Assert.Equal("test-vm", resources[1].Name);
            Assert.Equal(100, resources[1].VmId);
            Assert.Equal("storage/local", resources[2].Id);
            Assert.Equal("storage", resources[2].Type);
            mockClient.Verify(c => c.GetAsync("cluster/resources"), Times.Once);
        }

        [Fact]
        public void GetClusterResources_WithTypeFilter_AppendsQueryString()
        {
            // Arrange
            var json = @"{""data"": [
                {""id"": ""qemu/100"", ""type"": ""qemu"", ""node"": ""pve1"", ""name"": ""test-vm"", ""status"": ""running"", ""vmid"": 100}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/resources?type=vm")).ReturnsAsync(json);
            var service = new ClusterService(mockClient.Object);

            // Act
            var resources = service.GetClusterResources(CreateSession(), "vm");

            // Assert
            Assert.Single(resources);
            Assert.Equal("qemu/100", resources[0].Id);
            mockClient.Verify(c => c.GetAsync("cluster/resources?type=vm"), Times.Once);
        }

        [Fact]
        public void GetClusterStatus_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new ClusterService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetClusterStatus(null!));
        }

        [Fact]
        public void GetClusterResources_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new ClusterService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetClusterResources(null!));
        }

        [Fact]
        public void Constructor_NullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ClusterService(null!));
        }
    }
}
