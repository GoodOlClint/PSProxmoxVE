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
    public class ClusterConfigServiceTests
    {
        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        [Fact]
        public void GetClusterConfig_ReturnsJObject()
        {
            // Arrange
            var json = @"{""data"": {""nodes"": {""pve1"": {}}, ""totem"": {""version"": ""2""}}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/config")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var config = service.GetClusterConfig(CreateSession());

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config["nodes"]);
            Assert.NotNull(config["totem"]);
            mockClient.Verify(c => c.GetAsync("cluster/config"), Times.Once);
        }

        [Fact]
        public void CreateCluster_PostsClusterNameAndReturnsUpid()
        {
            // Arrange
            var json = @"{""data"": ""UPID:pve1:000ABC:00000001:5F1234AB:cluster_create::root@pam:""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "cluster/config",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var upid = service.CreateCluster(CreateSession(), "test-cluster");

            // Assert
            Assert.Contains("cluster_create", upid);
            mockClient.Verify(c => c.PostAsync(
                "cluster/config",
                It.Is<Dictionary<string, string>>(d =>
                    d["clustername"] == "test-cluster")),
                Times.Once);
        }

        [Fact]
        public void CreateCluster_WithOptionalParams_IncludesThemInPost()
        {
            // Arrange
            var json = @"{""data"": ""UPID:pve1:000ABC:00000001:5F1234AB:cluster_create::root@pam:""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "cluster/config",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);
            var links = new Dictionary<string, string> { ["link0"] = "10.0.0.1" };

            // Act
            service.CreateCluster(CreateSession(), "test-cluster", links, nodeid: 1, votes: 2);

            // Assert
            mockClient.Verify(c => c.PostAsync(
                "cluster/config",
                It.Is<Dictionary<string, string>>(d =>
                    d["clustername"] == "test-cluster" &&
                    d["link0"] == "10.0.0.1" &&
                    d["nodeid"] == "1" &&
                    d["votes"] == "2")),
                Times.Once);
        }

        [Fact]
        public void GetConfigNodes_ReturnsNodeArray()
        {
            // Arrange
            var json = @"{""data"": [
                {""node"": ""pve1"", ""nodeid"": 1, ""ring0_addr"": ""10.0.0.1"", ""quorum_votes"": 1},
                {""node"": ""pve2"", ""nodeid"": 2, ""ring0_addr"": ""10.0.0.2"", ""quorum_votes"": 1}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/config/nodes")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var nodes = service.GetConfigNodes(CreateSession());

            // Assert
            Assert.Equal(2, nodes.Length);
            Assert.Equal("pve1", nodes[0].Name);
            Assert.Equal(1, nodes[0].NodeId);
            Assert.Equal("10.0.0.1", nodes[0].Ring0Addr);
            Assert.Equal(1, nodes[0].QuorumVotes);
            Assert.Equal("pve2", nodes[1].Name);
            Assert.Equal(2, nodes[1].NodeId);
            mockClient.Verify(c => c.GetAsync("cluster/config/nodes"), Times.Once);
        }

        [Fact]
        public void AddConfigNode_PostsToEncodedUrlAndReturnsUpid()
        {
            // Arrange
            var json = @"{""data"": ""UPID:pve1:000ABC:00000001:5F1234AB:addnode::root@pam:""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "cluster/config/nodes/pve2",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var upid = service.AddConfigNode(CreateSession(), "pve2", newNodeIp: "10.0.0.2");

            // Assert
            Assert.Contains("addnode", upid);
            mockClient.Verify(c => c.PostAsync(
                "cluster/config/nodes/pve2",
                It.Is<Dictionary<string, string>>(d =>
                    d["new_node_ip"] == "10.0.0.2")),
                Times.Once);
        }

        [Fact]
        public void RemoveConfigNode_CallsDeleteWithEncodedUrl()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync("cluster/config/nodes/pve2"))
                .ReturnsAsync("{}");
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            service.RemoveConfigNode(CreateSession(), "pve2");

            // Assert
            mockClient.Verify(c => c.DeleteAsync("cluster/config/nodes/pve2"), Times.Once);
        }

        [Fact]
        public void GetJoinInfo_ReturnsClusterJoinInfo()
        {
            // Arrange
            var json = @"{""data"": {
                ""config_digest"": ""abc123"",
                ""preferred_node"": ""pve1"",
                ""nodelist"": [{""name"": ""pve1"", ""ring0_addr"": ""10.0.0.1""}],
                ""totem"": {""version"": ""2""}
            }}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/config/join")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var joinInfo = service.GetJoinInfo(CreateSession());

            // Assert
            Assert.Equal("abc123", joinInfo.ConfigDigest);
            Assert.Equal("pve1", joinInfo.PreferredNode);
            Assert.NotNull(joinInfo.Nodelist);
            Assert.Single(joinInfo.Nodelist);
            Assert.NotNull(joinInfo.Totem);
            mockClient.Verify(c => c.GetAsync("cluster/config/join"), Times.Once);
        }

        [Fact]
        public void GetJoinInfo_WithNode_AppendsQueryString()
        {
            // Arrange
            var json = @"{""data"": {""config_digest"": ""abc123"", ""preferred_node"": ""pve1""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/config/join?node=pve1")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var joinInfo = service.GetJoinInfo(CreateSession(), "pve1");

            // Assert
            Assert.Equal("pve1", joinInfo.PreferredNode);
            mockClient.Verify(c => c.GetAsync("cluster/config/join?node=pve1"), Times.Once);
        }

        [Fact]
        public void JoinCluster_PostsRequiredParamsAndReturnsUpid()
        {
            // Arrange
            var json = @"{""data"": ""UPID:pve2:000ABC:00000001:5F1234AB:join::root@pam:""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "cluster/config/join",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var upid = service.JoinCluster(CreateSession(), "10.0.0.1", "AA:BB:CC:DD", "secret");

            // Assert
            Assert.Contains("join", upid);
            mockClient.Verify(c => c.PostAsync(
                "cluster/config/join",
                It.Is<Dictionary<string, string>>(d =>
                    d["hostname"] == "10.0.0.1" &&
                    d["fingerprint"] == "AA:BB:CC:DD" &&
                    d["password"] == "secret")),
                Times.Once);
        }

        [Fact]
        public void GetTotem_ReturnsJObject()
        {
            // Arrange
            var json = @"{""data"": {""version"": ""2"", ""secauth"": ""on"", ""cluster_name"": ""pve-cluster""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/config/totem")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var totem = service.GetTotem(CreateSession());

            // Assert
            Assert.NotNull(totem);
            Assert.Equal("2", totem["version"]?.ToString());
            Assert.Equal("on", totem["secauth"]?.ToString());
            Assert.Equal("pve-cluster", totem["cluster_name"]?.ToString());
            mockClient.Verify(c => c.GetAsync("cluster/config/totem"), Times.Once);
        }

        [Fact]
        public void GetApiVersion_ReturnsInt()
        {
            // Arrange
            var json = @"{""data"": 10}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/config/apiversion")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var version = service.GetApiVersion(CreateSession());

            // Assert
            Assert.Equal(10, version);
            mockClient.Verify(c => c.GetAsync("cluster/config/apiversion"), Times.Once);
        }

        [Fact]
        public void GetClusterOptions_ReturnsPveClusterOptions()
        {
            // Arrange
            var json = @"{""data"": {
                ""keyboard"": ""en-us"",
                ""language"": ""en"",
                ""console"": ""html5"",
                ""fencing"": ""watchdog"",
                ""email_from"": ""admin@example.com"",
                ""max_workers"": 4
            }}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/options")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var options = service.GetClusterOptions(CreateSession());

            // Assert
            Assert.Equal("en-us", options.Keyboard);
            Assert.Equal("en", options.Language);
            Assert.Equal("html5", options.Console);
            Assert.Equal("watchdog", options.Fencing);
            Assert.Equal("admin@example.com", options.EmailFrom);
            Assert.Equal(4, options.MaxWorkers);
            mockClient.Verify(c => c.GetAsync("cluster/options"), Times.Once);
        }

        [Fact]
        public void SetClusterOptions_CallsPutAsyncWithCorrectResource()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PutAsync(
                    "cluster/options",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");
            var service = new ClusterConfigService(mockClient.Object);
            var opts = new Dictionary<string, string>
            {
                ["keyboard"] = "de",
                ["language"] = "de"
            };

            // Act
            service.SetClusterOptions(CreateSession(), opts);

            // Assert
            mockClient.Verify(c => c.PutAsync(
                "cluster/options",
                It.Is<Dictionary<string, string>>(d =>
                    d["keyboard"] == "de" && d["language"] == "de")),
                Times.Once);
        }

        [Fact]
        public void GetClusterStatus_ReturnsStatusArray()
        {
            // Arrange
            var json = @"{""data"": [
                {""type"": ""cluster"", ""name"": ""pve-cluster"", ""nodes"": 3, ""quorate"": 1, ""version"": 5},
                {""type"": ""node"", ""name"": ""pve1"", ""online"": 1, ""local"": 1, ""nodeid"": 1, ""ip"": ""10.0.0.1""}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/status")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var statuses = service.GetClusterStatus(CreateSession());

            // Assert
            Assert.Equal(2, statuses.Length);
            Assert.Equal("cluster", statuses[0].Type);
            Assert.Equal("pve-cluster", statuses[0].Name);
            Assert.Equal(3, statuses[0].Nodes);
            Assert.Equal(1, statuses[0].Quorate);
            Assert.Equal("node", statuses[1].Type);
            Assert.Equal("pve1", statuses[1].Name);
            Assert.Equal(1, statuses[1].Online);
            Assert.Equal("10.0.0.1", statuses[1].Ip);
            mockClient.Verify(c => c.GetAsync("cluster/status"), Times.Once);
        }

        [Fact]
        public void GetNextId_ReturnsInt()
        {
            // Arrange
            var json = @"{""data"": ""100""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/nextid")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var nextId = service.GetNextId(CreateSession());

            // Assert
            Assert.Equal(100, nextId);
            mockClient.Verify(c => c.GetAsync("cluster/nextid"), Times.Once);
        }

        [Fact]
        public void GetNextId_WithVmid_AppendsQueryString()
        {
            // Arrange
            var json = @"{""data"": ""200""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/nextid?vmid=200")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act
            var nextId = service.GetNextId(CreateSession(), 200);

            // Assert
            Assert.Equal(200, nextId);
            mockClient.Verify(c => c.GetAsync("cluster/nextid?vmid=200"), Times.Once);
        }

        [Fact]
        public void GetNextId_InvalidResponse_ThrowsInvalidOperationException()
        {
            // Arrange
            var json = @"{""data"": ""not-a-number""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/nextid")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => service.GetNextId(CreateSession()));
            Assert.Contains("unexpected next VMID value", ex.Message);
        }

        [Fact]
        public void GetNextId_NullDataField_ThrowsInvalidOperationException()
        {
            // Arrange
            var json = @"{""notdata"": ""100""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/nextid")).ReturnsAsync(json);
            var service = new ClusterConfigService(mockClient.Object);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => service.GetNextId(CreateSession()));
            Assert.Contains("did not contain a 'data' field", ex.Message);
        }

        [Fact]
        public void GetClusterConfig_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new ClusterConfigService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetClusterConfig(null!));
        }

        [Fact]
        public void GetConfigNodes_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new ClusterConfigService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetConfigNodes(null!));
        }

        [Fact]
        public void GetClusterOptions_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new ClusterConfigService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetClusterOptions(null!));
        }

        [Fact]
        public void GetNextId_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new ClusterConfigService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetNextId(null!));
        }

        [Fact]
        public void GetClusterStatus_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new ClusterConfigService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetClusterStatus(null!));
        }

        [Fact]
        public void Constructor_NullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ClusterConfigService(null!));
        }
    }
}
