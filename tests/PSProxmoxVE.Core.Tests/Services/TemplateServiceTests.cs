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
        public void CreateTemplate_NullSession_ThrowsArgumentNullException()
        {
            var service = new TemplateService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.CreateTemplate(null!, Node, VmId));
        }

        [Fact]
        public void CreateTemplate_NullNode_ThrowsArgumentNullException()
        {
            var service = new TemplateService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("node", () => service.CreateTemplate(CreateSession(), null!, VmId));
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
        public void GetTemplates_NullSession_ThrowsArgumentNullException()
        {
            var service = new TemplateService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.GetTemplates(null!, Node));
        }

        [Fact]
        public void GetTemplates_AllNodes_AggregatesAcrossNodesAndStampsNode()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes"))
                .ReturnsAsync(@"{""data"": [{""node"": ""pve1""}, {""node"": ""pve2""}]}");
            mockClient.Setup(c => c.GetAsync("nodes/pve1/qemu"))
                .ReturnsAsync(@"{""data"": [{""vmid"": 100, ""template"": 1}, {""vmid"": 101, ""template"": 0}]}");
            mockClient.Setup(c => c.GetAsync("nodes/pve2/qemu"))
                .ReturnsAsync(@"{""data"": [{""vmid"": 200, ""template"": 1}]}");
            var service = new TemplateService(mockClient.Object);

            // Act
            var templates = service.GetTemplates(CreateSession());

            // Assert
            Assert.Equal(2, templates.Length);
            Assert.Contains(templates, t => t.VmId == 100 && t.Node == "pve1");
            Assert.Contains(templates, t => t.VmId == 200 && t.Node == "pve2");
        }

        [Fact]
        public void GetTemplates_AllNodes_UnreachableNodeIsSkippedAndReported()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes"))
                .ReturnsAsync(@"{""data"": [{""node"": ""pve1""}, {""node"": ""pve2""}]}");
            mockClient.Setup(c => c.GetAsync("nodes/pve1/qemu"))
                .ReturnsAsync(@"{""data"": [{""vmid"": 100, ""template"": 1}]}");
            mockClient.Setup(c => c.GetAsync("nodes/pve2/qemu"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.InternalServerError, "internal error", "nodes/pve2/qemu", "GET"));
            var service = new TemplateService(mockClient.Object);

            // Act
            var skipped = new List<string>();
            var templates = service.GetTemplates(CreateSession(), onNodeSkipped: (node, ex) => skipped.Add(node));

            // Assert
            var template = Assert.Single(templates);
            Assert.Equal(100, template.VmId);
            Assert.Equal(new[] { "pve2" }, skipped);
        }

        [Fact]
        public void GetTemplates_AllNodes_PermissionErrorOnOneNodePropagates()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("nodes"))
                .ReturnsAsync(@"{""data"": [{""node"": ""pve1""}, {""node"": ""pve2""}]}");
            mockClient.Setup(c => c.GetAsync("nodes/pve1/qemu"))
                .ReturnsAsync(@"{""data"": [{""vmid"": 100, ""template"": 1}]}");
            mockClient.Setup(c => c.GetAsync("nodes/pve2/qemu"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.Forbidden, "permission denied", "nodes/pve2/qemu", "GET"));
            var service = new TemplateService(mockClient.Object);

            // Act & Assert
            Assert.Throws<PveApiException>(() => service.GetTemplates(CreateSession()));
        }
    }
}
