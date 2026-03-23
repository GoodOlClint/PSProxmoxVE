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
    }
}
