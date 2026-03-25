using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Models.HA;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class HaServiceTests
    {
        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        // -----------------------------------------------------------------
        // Resources
        // -----------------------------------------------------------------

        [Fact]
        public void GetResources_ReturnsResourceArray()
        {
            // Arrange
            var json = @"{""data"": [
                {""sid"": ""vm:100"", ""state"": ""started"", ""group"": ""grp1"", ""type"": ""vm"", ""max_relocate"": 1, ""max_restart"": 1},
                {""sid"": ""ct:200"", ""state"": ""stopped"", ""type"": ""ct""}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/ha/resources")).ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var resources = service.GetResources(CreateSession());

            // Assert
            Assert.Equal(2, resources.Length);
            Assert.Equal("vm:100", resources[0].Sid);
            Assert.Equal("started", resources[0].State);
            Assert.Equal("grp1", resources[0].Group);
            Assert.Equal("vm", resources[0].Type);
            Assert.Equal(1, resources[0].MaxRelocate);
            Assert.Equal(1, resources[0].MaxRestart);
            Assert.Equal("ct:200", resources[1].Sid);
            Assert.Equal("stopped", resources[1].State);
            mockClient.Verify(c => c.GetAsync("cluster/ha/resources"), Times.Once);
        }

        [Fact]
        public void GetResource_ReturnsSingleResource()
        {
            // Arrange
            var json = @"{""data"": {""sid"": ""vm:100"", ""state"": ""started"", ""group"": ""grp1"", ""type"": ""vm""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/ha/resources/vm%3A100")).ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var resource = service.GetResource(CreateSession(), "vm:100");

            // Assert
            Assert.Equal("vm:100", resource.Sid);
            Assert.Equal("started", resource.State);
            Assert.Equal("grp1", resource.Group);
            Assert.Equal("vm", resource.Type);
            mockClient.Verify(c => c.GetAsync("cluster/ha/resources/vm%3A100"), Times.Once);
        }

        [Fact]
        public void GetResource_EncodesColonInSid()
        {
            // Arrange
            var json = @"{""data"": {""sid"": ""ct:200"", ""state"": ""stopped""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/ha/resources/ct%3A200")).ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var resource = service.GetResource(CreateSession(), "ct:200");

            // Assert
            Assert.Equal("ct:200", resource.Sid);
            mockClient.Verify(c => c.GetAsync("cluster/ha/resources/ct%3A200"), Times.Once);
        }

        [Fact]
        public void CreateResource_PostsSidAndOptions()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "cluster/ha/resources",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");
            var service = new HaService(mockClient.Object);
            var options = new Dictionary<string, string>
            {
                ["state"] = "started",
                ["group"] = "grp1"
            };

            // Act
            service.CreateResource(CreateSession(), "vm:100", options);

            // Assert
            mockClient.Verify(c => c.PostAsync(
                "cluster/ha/resources",
                It.Is<Dictionary<string, string>>(d =>
                    d["sid"] == "vm:100" &&
                    d["state"] == "started" &&
                    d["group"] == "grp1")),
                Times.Once);
        }

        [Fact]
        public void UpdateResource_PutsOptionsToEncodedUrl()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PutAsync(
                    "cluster/ha/resources/vm%3A100",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");
            var service = new HaService(mockClient.Object);
            var options = new Dictionary<string, string>
            {
                ["state"] = "stopped"
            };

            // Act
            service.UpdateResource(CreateSession(), "vm:100", options);

            // Assert
            mockClient.Verify(c => c.PutAsync(
                "cluster/ha/resources/vm%3A100",
                It.Is<Dictionary<string, string>>(d =>
                    d["state"] == "stopped")),
                Times.Once);
        }

        [Fact]
        public void DeleteResource_CallsDeleteWithEncodedUrl()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync("cluster/ha/resources/vm%3A100"))
                .ReturnsAsync("{}");
            var service = new HaService(mockClient.Object);

            // Act
            service.DeleteResource(CreateSession(), "vm:100");

            // Assert
            mockClient.Verify(c => c.DeleteAsync("cluster/ha/resources/vm%3A100"), Times.Once);
        }

        [Fact]
        public void MigrateResource_PostsNodeToEncodedUrl()
        {
            // Arrange
            var json = @"{""data"": ""UPID:pve1:000ABC:00000001:5F1234AB:hamigrate::root@pam:""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "cluster/ha/resources/vm%3A100/migrate",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var upid = service.MigrateResource(CreateSession(), "vm:100", "pve2");

            // Assert
            Assert.Contains("hamigrate", upid);
            mockClient.Verify(c => c.PostAsync(
                "cluster/ha/resources/vm%3A100/migrate",
                It.Is<Dictionary<string, string>>(d =>
                    d["node"] == "pve2")),
                Times.Once);
        }

        [Fact]
        public void RelocateResource_PostsNodeToEncodedUrl()
        {
            // Arrange
            var json = @"{""data"": ""UPID:pve1:000DEF:00000002:5F1234AC:harelocate::root@pam:""}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "cluster/ha/resources/vm%3A100/relocate",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var upid = service.RelocateResource(CreateSession(), "vm:100", "pve3");

            // Assert
            Assert.Contains("harelocate", upid);
            mockClient.Verify(c => c.PostAsync(
                "cluster/ha/resources/vm%3A100/relocate",
                It.Is<Dictionary<string, string>>(d =>
                    d["node"] == "pve3")),
                Times.Once);
        }

        // -----------------------------------------------------------------
        // Groups
        // -----------------------------------------------------------------

        [Fact]
        public void GetGroups_ReturnsGroupArray()
        {
            // Arrange
            var json = @"{""data"": [
                {""group"": ""grp1"", ""nodes"": ""pve1:2,pve2:1"", ""restricted"": 1, ""nofailback"": 0, ""comment"": ""Primary group""},
                {""group"": ""grp2"", ""nodes"": ""pve2,pve3""}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/ha/groups")).ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var groups = service.GetGroups(CreateSession());

            // Assert
            Assert.Equal(2, groups.Length);
            Assert.Equal("grp1", groups[0].Group);
            Assert.Equal("pve1:2,pve2:1", groups[0].Nodes);
            Assert.Equal(1, groups[0].Restricted);
            Assert.Equal(0, groups[0].NoFailback);
            Assert.Equal("Primary group", groups[0].Comment);
            Assert.Equal("grp2", groups[1].Group);
            Assert.Equal("pve2,pve3", groups[1].Nodes);
            mockClient.Verify(c => c.GetAsync("cluster/ha/groups"), Times.Once);
        }

        [Fact]
        public void GetGroup_ReturnsSingleGroup()
        {
            // Arrange
            var json = @"{""data"": {""group"": ""grp1"", ""nodes"": ""pve1:2,pve2:1"", ""restricted"": 0}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/ha/groups/grp1")).ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var group = service.GetGroup(CreateSession(), "grp1");

            // Assert
            Assert.Equal("grp1", group.Group);
            Assert.Equal("pve1:2,pve2:1", group.Nodes);
            Assert.Equal(0, group.Restricted);
            mockClient.Verify(c => c.GetAsync("cluster/ha/groups/grp1"), Times.Once);
        }

        [Fact]
        public void CreateGroup_PostsGroupNodesAndOptions()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "cluster/ha/groups",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");
            var service = new HaService(mockClient.Object);
            var options = new Dictionary<string, string>
            {
                ["restricted"] = "1",
                ["comment"] = "Test group"
            };

            // Act
            service.CreateGroup(CreateSession(), "grp1", "pve1:2,pve2:1", options);

            // Assert
            mockClient.Verify(c => c.PostAsync(
                "cluster/ha/groups",
                It.Is<Dictionary<string, string>>(d =>
                    d["group"] == "grp1" &&
                    d["nodes"] == "pve1:2,pve2:1" &&
                    d["restricted"] == "1" &&
                    d["comment"] == "Test group")),
                Times.Once);
        }

        [Fact]
        public void DeleteGroup_CallsDeleteWithEncodedUrl()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync("cluster/ha/groups/grp1"))
                .ReturnsAsync("{}");
            var service = new HaService(mockClient.Object);

            // Act
            service.DeleteGroup(CreateSession(), "grp1");

            // Assert
            mockClient.Verify(c => c.DeleteAsync("cluster/ha/groups/grp1"), Times.Once);
        }

        // -----------------------------------------------------------------
        // Status
        // -----------------------------------------------------------------

        [Fact]
        public void GetStatus_ReturnsStatusArray()
        {
            // Arrange
            var json = @"{""data"": [
                {""id"": ""quorum"", ""type"": ""quorum"", ""node"": ""pve1"", ""status"": ""OK"", ""timestamp"": 1700000000},
                {""id"": ""manager"", ""type"": ""manager"", ""node"": ""pve1"", ""status"": ""OK"", ""crm_state"": ""S_IDLE""},
                {""id"": ""service:vm:100"", ""type"": ""service"", ""node"": ""pve1"", ""status"": ""started"", ""request_state"": ""started""}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/ha/status/current")).ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var statuses = service.GetStatus(CreateSession());

            // Assert
            Assert.Equal(3, statuses.Length);
            Assert.Equal("quorum", statuses[0].Id);
            Assert.Equal("quorum", statuses[0].Type);
            Assert.Equal("pve1", statuses[0].Node);
            Assert.Equal("OK", statuses[0].Status);
            Assert.Equal(1700000000L, statuses[0].Timestamp);
            Assert.Equal("manager", statuses[1].Id);
            Assert.Equal("S_IDLE", statuses[1].CrmState);
            Assert.Equal("service:vm:100", statuses[2].Id);
            Assert.Equal("started", statuses[2].RequestState);
            mockClient.Verify(c => c.GetAsync("cluster/ha/status/current"), Times.Once);
        }

        [Fact]
        public void GetManagerStatus_ReturnsJObject()
        {
            // Arrange
            var json = @"{""data"": {""manager_status"": {""master_node"": ""pve1""}, ""quorum"": {""node"": ""pve1""}}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/ha/status/manager_status")).ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var status = service.GetManagerStatus(CreateSession());

            // Assert
            Assert.NotNull(status);
            Assert.NotNull(status["manager_status"]);
            Assert.NotNull(status["quorum"]);
            mockClient.Verify(c => c.GetAsync("cluster/ha/status/manager_status"), Times.Once);
        }

        // -----------------------------------------------------------------
        // Rules
        // -----------------------------------------------------------------

        [Fact]
        public void GetRules_ReturnsRuleArray()
        {
            // Arrange
            var json = @"{""data"": [
                {""rule"": ""rule1"", ""type"": ""location"", ""state"": ""enabled"", ""comment"": ""Location rule""},
                {""rule"": ""rule2"", ""type"": ""colocation"", ""state"": ""disabled""}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/ha/rules")).ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var rules = service.GetRules(CreateSession());

            // Assert
            Assert.Equal(2, rules.Length);
            Assert.Equal("rule1", rules[0].Rule);
            Assert.Equal("location", rules[0].Type);
            Assert.Equal("enabled", rules[0].State);
            Assert.Equal("Location rule", rules[0].Comment);
            Assert.Equal("rule2", rules[1].Rule);
            Assert.Equal("colocation", rules[1].Type);
            Assert.Equal("disabled", rules[1].State);
            mockClient.Verify(c => c.GetAsync("cluster/ha/rules"), Times.Once);
        }

        [Fact]
        public void GetRule_ReturnsSingleRule()
        {
            // Arrange
            var json = @"{""data"": {""rule"": ""rule1"", ""type"": ""location"", ""state"": ""enabled""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/ha/rules/rule1")).ReturnsAsync(json);
            var service = new HaService(mockClient.Object);

            // Act
            var rule = service.GetRule(CreateSession(), "rule1");

            // Assert
            Assert.Equal("rule1", rule.Rule);
            Assert.Equal("location", rule.Type);
            Assert.Equal("enabled", rule.State);
            mockClient.Verify(c => c.GetAsync("cluster/ha/rules/rule1"), Times.Once);
        }

        [Fact]
        public void CreateRule_PostsOptions()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(
                    "cluster/ha/rules",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync("{}");
            var service = new HaService(mockClient.Object);
            var options = new Dictionary<string, string>
            {
                ["rule"] = "rule1",
                ["type"] = "location",
                ["state"] = "enabled"
            };

            // Act
            service.CreateRule(CreateSession(), options);

            // Assert
            mockClient.Verify(c => c.PostAsync(
                "cluster/ha/rules",
                It.Is<Dictionary<string, string>>(d =>
                    d["rule"] == "rule1" &&
                    d["type"] == "location" &&
                    d["state"] == "enabled")),
                Times.Once);
        }

        [Fact]
        public void DeleteRule_CallsDeleteWithEncodedUrl()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync("cluster/ha/rules/rule1"))
                .ReturnsAsync("{}");
            var service = new HaService(mockClient.Object);

            // Act
            service.DeleteRule(CreateSession(), "rule1");

            // Assert
            mockClient.Verify(c => c.DeleteAsync("cluster/ha/rules/rule1"), Times.Once);
        }

        // -----------------------------------------------------------------
        // Null session tests
        // -----------------------------------------------------------------

        [Fact]
        public void GetResources_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new HaService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetResources(null!));
        }

        [Fact]
        public void GetGroups_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new HaService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetGroups(null!));
        }

        [Fact]
        public void GetStatus_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new HaService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetStatus(null!));
        }

        [Fact]
        public void GetRules_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new HaService(mockClient.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.GetRules(null!));
        }

        [Fact]
        public void Constructor_NullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new HaService(null!));
        }
    }
}
