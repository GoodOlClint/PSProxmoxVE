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
    public class PoolServiceTests
    {
        private static PveSession CreateSession()
        {
            return new PveSession("pve1.example.com", 8006, true, "PVE:root@pam:TEST_TOKEN");
        }

        [Fact]
        public void GetPools_HappyPath_ReturnsArray()
        {
            // Arrange
            var json = @"{
                ""data"": [
                    { ""poolid"": ""dev-pool"", ""comment"": ""Development resources"" },
                    { ""poolid"": ""prod-pool"", ""comment"": ""Production resources"" }
                ]
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("pools"))
                .ReturnsAsync(json);

            var service = new PoolService(mockClient.Object);
            var session = CreateSession();

            // Act
            var pools = service.GetPools(session);

            // Assert
            Assert.Equal(2, pools.Length);
            Assert.Equal("dev-pool", pools[0].PoolId);
            Assert.Equal("Development resources", pools[0].Comment);
            Assert.Equal("prod-pool", pools[1].PoolId);
        }

        [Fact]
        public void GetPool_HappyPath_ReturnsSinglePool()
        {
            // Arrange
            var json = @"{
                ""data"": {
                    ""poolid"": ""dev-pool"",
                    ""comment"": ""Development resources"",
                    ""members"": [
                        { ""id"": ""qemu/100"", ""node"": ""pve1"", ""type"": ""qemu"", ""vmid"": 100 }
                    ]
                }
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("pools/dev-pool"))
                .ReturnsAsync(json);

            var service = new PoolService(mockClient.Object);
            var session = CreateSession();

            // Act
            var pool = service.GetPool(session, "dev-pool");

            // Assert
            Assert.NotNull(pool);
            Assert.Equal("dev-pool", pool.PoolId);
            Assert.Equal("Development resources", pool.Comment);
            Assert.NotNull(pool.Members);
            Assert.Single(pool.Members);
        }

        [Fact]
        public void CreatePool_CallsPostAsync()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync("pools", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");

            var service = new PoolService(mockClient.Object);
            var session = CreateSession();

            // Act
            service.CreatePool(session, "test-pool", "A test pool");

            // Assert
            mockClient.Verify(c => c.PostAsync("pools",
                It.Is<Dictionary<string, string>>(d =>
                    d["poolid"] == "test-pool" &&
                    d["comment"] == "A test pool")),
                Times.Once);
        }

        [Fact]
        public void UpdatePool_CallsPutAsync()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");

            var service = new PoolService(mockClient.Object);
            var session = CreateSession();

            var config = new Dictionary<string, string>
            {
                { "comment", "Updated comment" }
            };

            // Act
            service.UpdatePool(session, "dev-pool", config);

            // Assert
            mockClient.Verify(c => c.PutAsync("pools/dev-pool",
                It.Is<Dictionary<string, string>>(d =>
                    d["comment"] == "Updated comment")),
                Times.Once);
        }

        [Fact]
        public void RemovePool_CallsDeleteAsync()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync("{}");

            var service = new PoolService(mockClient.Object);
            var session = CreateSession();

            // Act
            service.RemovePool(session, "dev-pool");

            // Assert
            mockClient.Verify(c => c.DeleteAsync("pools/dev-pool"), Times.Once);
        }
    }
}
