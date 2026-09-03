using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Moq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class TemplateServiceTests
    {
        private const string Node = "pve1";
        private const int VmId = 9000;

        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        [Fact]
        public void CreateTemplate_CallsPostAsync_ReturnsUpid()
        {
            // Arrange
            const string upid = "UPID:pve1:000ABC:00000001:5F1234AB:qmtemplate:9000:root@pam:";
            var json = $@"{{""data"": ""{upid}""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);

            var service = new TemplateService(mockClient.Object);

            // Act
            var task = service.CreateTemplate(CreateSession(), Node, VmId);

            // Assert
            Assert.Equal(upid, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Equal("running", task.Status);
            mockClient.Verify(c => c.PostAsync(
                $"nodes/{Node}/qemu/{VmId}/template",
                It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Fact]
        public void CreateTemplate_WithTaskObject_ParsesCorrectly()
        {
            // Arrange — some PVE versions return a full task object instead of a bare UPID string
            var json = @"{
                ""data"": {
                    ""upid"": ""UPID:pve1:000ABC:00000001:5F1234AB:qmtemplate:9000:root@pam:"",
                    ""type"": ""qmtemplate"",
                    ""status"": ""running"",
                    ""node"": ""pve1"",
                    ""user"": ""root@pam""
                }
            }";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);

            var service = new TemplateService(mockClient.Object);

            // Act
            var task = service.CreateTemplate(CreateSession(), Node, VmId);

            // Assert
            Assert.Equal("UPID:pve1:000ABC:00000001:5F1234AB:qmtemplate:9000:root@pam:", task.Upid);
            Assert.Equal("qmtemplate", task.Type);
            Assert.Equal(Node, task.Node);
        }

        [Fact]
        public void CreateTemplate_EmptyNode_ThrowsArgumentNullException()
        {
            var service = new TemplateService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("node", () => service.CreateTemplate(CreateSession(), "  ", VmId));
        }

        [Fact]
        public void Constructor_NullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("client", () => new TemplateService(null!));
        }

        [Fact]
        public void GetTemplates_SingleNode_ReturnsOnlyTemplateFlaggedVms()
        {
            // Arrange
            var json = @"{""data"": [
                {""vmid"": 100, ""name"": ""web-template"", ""template"": 1},
                {""vmid"": 101, ""name"": ""running-vm"", ""template"": 0},
                {""vmid"": 102, ""name"": ""db-template"", ""template"": 1}
            ]}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync($"nodes/{Node}/qemu")).ReturnsAsync(json);
            var service = new TemplateService(mockClient.Object);

            // Act
            var templates = service.GetTemplates(CreateSession(), Node);

            // Assert
            Assert.Equal(2, templates.Length);
            Assert.All(templates, t => Assert.Equal(1, t.Template));
            Assert.Contains(templates, t => t.VmId == 100);
            Assert.Contains(templates, t => t.VmId == 102);
            mockClient.Verify(c => c.GetAsync($"nodes/{Node}/qemu"), Times.Once);
        }

        [Fact]
        public void GetTemplates_EscapesNodeInPath()
        {
            // Arrange
            var json = @"{""data"": []}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes/pve%20node/qemu")).ReturnsAsync(json);
            var service = new TemplateService(mockClient.Object);

            // Act
            service.GetTemplates(CreateSession(), "pve node");

            // Assert
            mockClient.Verify(c => c.GetAsync("nodes/pve%20node/qemu"), Times.Once);
        }

        [Fact]
        public void GetTemplates_AllNodes_SourcesFromClusterResourcesInOneCall()
        {
            // Arrange: issue #152 — VmService.GetVms(node: null) now sources the all-nodes
            // listing from cluster/resources instead of a call per node.
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/resources?type=vm"))
                .ReturnsAsync(@"{""data"": [
                    {""type"": ""qemu"", ""vmid"": 100, ""node"": ""pve1"", ""template"": 1},
                    {""type"": ""qemu"", ""vmid"": 101, ""node"": ""pve1"", ""template"": 0},
                    {""type"": ""qemu"", ""vmid"": 200, ""node"": ""pve2"", ""template"": 1},
                    {""type"": ""lxc"", ""vmid"": 300, ""node"": ""pve2"", ""template"": 1}
                ]}");
            var service = new TemplateService(mockClient.Object);

            // Act
            var templates = service.GetTemplates(CreateSession());

            // Assert
            Assert.Equal(2, templates.Length);
            Assert.Contains(templates, t => t.VmId == 100 && t.Node == "pve1");
            Assert.Contains(templates, t => t.VmId == 200 && t.Node == "pve2");
            Assert.DoesNotContain(templates, t => t.VmId == 300);
            mockClient.Verify(c => c.GetAsync("cluster/resources?type=vm"), Times.Once);
        }
    }
}
