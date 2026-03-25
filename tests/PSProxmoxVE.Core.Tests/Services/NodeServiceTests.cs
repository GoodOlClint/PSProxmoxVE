using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class NodeServiceTests
    {
        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        [Fact]
        public void GetNodes_ReturnsArrayOfPveNode()
        {
            // Arrange
            var json = @"{""data"": [
                {""node"": ""pve1"", ""status"": ""online"", ""maxcpu"": 16, ""maxmem"": 68719476736},
                {""node"": ""pve2"", ""status"": ""online"", ""maxcpu"": 8, ""maxmem"": 34359738368}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes")).ReturnsAsync(json);
            var service = new NodeService(mockClient.Object);

            // Act
            var nodes = service.GetNodes(CreateSession());

            // Assert
            Assert.Equal(2, nodes.Length);
            Assert.Equal("pve1", nodes[0].Name);
            Assert.Equal("online", nodes[0].Status);
            Assert.Equal(16, nodes[0].CpuCount);
            Assert.Equal(68719476736L, nodes[0].MemoryTotal);
            Assert.Equal("pve2", nodes[1].Name);
            Assert.Equal(8, nodes[1].CpuCount);
            mockClient.Verify(c => c.GetAsync("nodes"), Times.Once);
        }

        [Fact]
        public void GetNodeStatus_ReturnsPveNodeStatus()
        {
            // Arrange
            var json = @"{""data"": {
                ""node"": ""pve1"", ""status"": ""online"", ""maxcpu"": 16,
                ""maxmem"": 68719476736, ""mem"": 17179869184,
                ""uptime"": 864000, ""cpu"": 0.125
            }}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes/pve1/status")).ReturnsAsync(json);
            var service = new NodeService(mockClient.Object);

            // Act
            var status = service.GetNodeStatus(CreateSession(), "pve1");

            // Assert
            Assert.Equal("pve1", status.Node);
            Assert.Equal("online", status.Status);
            Assert.Equal(0.125, status.CpuUsage);
            Assert.Equal(68719476736L, status.MemoryTotal);
            Assert.Equal(17179869184L, status.MemoryUsed);
            Assert.Equal(864000L, status.Uptime);
            mockClient.Verify(c => c.GetAsync("nodes/pve1/status"), Times.Once);
        }

        [Fact]
        public void GetNodeConfig_ReturnsDictionary()
        {
            // Arrange
            var json = @"{""data"": {""description"": ""Primary node"", ""wakeonlan"": ""AA:BB:CC:DD:EE:FF""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes/pve1/config")).ReturnsAsync(json);
            var service = new NodeService(mockClient.Object);

            // Act
            var config = service.GetNodeConfig(CreateSession(), "pve1");

            // Assert
            Assert.NotNull(config);
            Assert.IsType<Dictionary<string, object?>>(config);
            Assert.Equal("Primary node", config["description"]?.ToString());
            Assert.Equal("AA:BB:CC:DD:EE:FF", config["wakeonlan"]?.ToString());
            mockClient.Verify(c => c.GetAsync("nodes/pve1/config"), Times.Once);
        }

        [Fact]
        public void SetNodeConfig_CallsPutAsyncWithCorrectResource()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PutAsync(
                    "nodes/pve1/config",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");
            var service = new NodeService(mockClient.Object);
            var config = new Dictionary<string, string>
            {
                ["description"] = "Updated node"
            };

            // Act
            service.SetNodeConfig(CreateSession(), "pve1", config);

            // Assert
            mockClient.Verify(c => c.PutAsync(
                "nodes/pve1/config",
                It.Is<Dictionary<string, string>>(d =>
                    d["description"] == "Updated node")),
                Times.Once);
        }

        [Fact]
        public void GetNodeDns_ReturnsDictionary()
        {
            // Arrange
            var json = @"{""data"": {""dns1"": ""8.8.8.8"", ""dns2"": ""8.8.4.4"", ""search"": ""example.com""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes/pve1/dns")).ReturnsAsync(json);
            var service = new NodeService(mockClient.Object);

            // Act
            var dns = service.GetNodeDns(CreateSession(), "pve1");

            // Assert
            Assert.NotNull(dns);
            Assert.IsType<Dictionary<string, object?>>(dns);
            Assert.Equal("8.8.8.8", dns["dns1"]?.ToString());
            Assert.Equal("8.8.4.4", dns["dns2"]?.ToString());
            Assert.Equal("example.com", dns["search"]?.ToString());
            mockClient.Verify(c => c.GetAsync("nodes/pve1/dns"), Times.Once);
        }

        [Fact]
        public void SetNodeDns_CallsPutAsyncWithCorrectResource()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PutAsync(
                    "nodes/pve1/dns",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");
            var service = new NodeService(mockClient.Object);
            var config = new Dictionary<string, string>
            {
                ["dns1"] = "1.1.1.1",
                ["search"] = "lab.local"
            };

            // Act
            service.SetNodeDns(CreateSession(), "pve1", config);

            // Assert
            mockClient.Verify(c => c.PutAsync(
                "nodes/pve1/dns",
                It.Is<Dictionary<string, string>>(d =>
                    d["dns1"] == "1.1.1.1" && d["search"] == "lab.local")),
                Times.Once);
        }

        [Fact]
        public void StartAll_CallsPostAsync()
        {
            // Arrange
            var json = @"{""data"": ""UPID:pve1:000ABC:00000001:5F1234AB:startall::root@pam:""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "nodes/pve1/startall",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);
            var service = new NodeService(mockClient.Object);

            // Act
            var task = service.StartAll(CreateSession(), "pve1");

            // Assert
            Assert.NotNull(task);
            Assert.Contains("startall", task.Upid);
            Assert.Equal("pve1", task.Node);
            mockClient.Verify(c => c.PostAsync(
                "nodes/pve1/startall",
                It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Fact]
        public void StopAll_CallsPostAsync()
        {
            // Arrange
            var json = @"{""data"": ""UPID:pve1:000DEF:00000002:5F1234AC:stopall::root@pam:""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "nodes/pve1/stopall",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);
            var service = new NodeService(mockClient.Object);

            // Act
            var task = service.StopAll(CreateSession(), "pve1");

            // Assert
            Assert.NotNull(task);
            Assert.Contains("stopall", task.Upid);
            Assert.Equal("pve1", task.Node);
            mockClient.Verify(c => c.PostAsync(
                "nodes/pve1/stopall",
                It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetNodes_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new NodeService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetNodes(null!));
        }

        [Fact]
        public void GetNodeStatus_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new NodeService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetNodeStatus(null!, "pve1"));
        }

        [Fact]
        public void GetNodeStatus_NullNode_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new NodeService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetNodeStatus(CreateSession(), null!));
        }

        [Fact]
        public void Constructor_NullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new NodeService(null!));
        }
    }
}
