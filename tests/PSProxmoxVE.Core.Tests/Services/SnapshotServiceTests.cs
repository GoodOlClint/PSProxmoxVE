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
    public class SnapshotServiceTests
    {
        private const string Node = "pve1";
        private const int VmId = 100;

        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        [Fact]
        public void GetSnapshots_ReturnsSnapshotArray()
        {
            // Arrange
            var json = @"{
                ""data"": [
                    {
                        ""name"": ""clean-install"",
                        ""description"": ""Fresh OS install"",
                        ""snaptime"": 1700000000,
                        ""vmstate"": 0,
                        ""parent"": null
                    },
                    {
                        ""name"": ""post-update"",
                        ""description"": ""After apt upgrade"",
                        ""snaptime"": 1700100000,
                        ""vmstate"": 1,
                        ""parent"": ""clean-install""
                    }
                ]
            }";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new SnapshotService(mockClient.Object);
            var session = CreateSession();

            // Act
            var snapshots = service.GetSnapshots(session, Node, VmId);

            // Assert
            Assert.Equal(2, snapshots.Length);
            Assert.Equal("clean-install", snapshots[0].Name);
            Assert.Equal("Fresh OS install", snapshots[0].Description);
            Assert.Equal(1700000000L, snapshots[0].SnapTime);
            Assert.Equal(0, snapshots[0].VmState);
            Assert.Null(snapshots[0].Parent);

            Assert.Equal("post-update", snapshots[1].Name);
            Assert.Equal(1, snapshots[1].VmState);
            Assert.Equal("clean-install", snapshots[1].Parent);

            mockClient.Verify(c => c.GetAsync($"nodes/{Node}/qemu/{VmId}/snapshot"), Times.Once);
        }

        [Fact]
        public void GetSnapshots_EmptyData_ReturnsEmptyArray()
        {
            // Arrange
            var json = @"{""data"": []}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new SnapshotService(mockClient.Object);

            // Act
            var snapshots = service.GetSnapshots(CreateSession(), Node, VmId);

            // Assert
            Assert.Empty(snapshots);
        }

        [Fact]
        public void CreateSnapshot_CallsPostAsync_ReturnsUpid()
        {
            // Arrange
            const string upid = "UPID:pve1:000ABC:00000001:5F1234AB:qmsnapshot:100:root@pam:";
            var json = $@"{{""data"": ""{upid}""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);

            var service = new SnapshotService(mockClient.Object);

            // Act
            var task = service.CreateSnapshot(CreateSession(), Node, VmId, "my-snap", "Test snapshot", vmstate: true);

            // Assert
            Assert.Equal(upid, task.Upid);
            Assert.Equal(Node, task.Node);
            mockClient.Verify(c => c.PostAsync(
                $"nodes/{Node}/qemu/{VmId}/snapshot",
                It.Is<Dictionary<string, string>>(d =>
                    d["snapname"] == "my-snap" &&
                    d["vmstate"] == "1" &&
                    d["description"] == "Test snapshot")),
                Times.Once);
        }

        [Fact]
        public void CreateSnapshot_WithoutDescription_OmitsDescriptionField()
        {
            // Arrange
            const string upid = "UPID:pve1:000ABC:00000001:5F1234AB:qmsnapshot:100:root@pam:";
            var json = $@"{{""data"": ""{upid}""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);

            var service = new SnapshotService(mockClient.Object);

            // Act
            service.CreateSnapshot(CreateSession(), Node, VmId, "my-snap");

            // Assert
            mockClient.Verify(c => c.PostAsync(
                It.IsAny<string>(),
                It.Is<Dictionary<string, string>>(d =>
                    !d.ContainsKey("description") &&
                    d["vmstate"] == "0")),
                Times.Once);
        }

        [Fact]
        public void RemoveSnapshot_CallsDeleteAsync_ReturnsUpid()
        {
            // Arrange
            const string upid = "UPID:pve1:000DEF:00000002:5F1234AC:qmdelsnap:100:root@pam:";
            var json = $@"{{""data"": ""{upid}""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new SnapshotService(mockClient.Object);

            // Act
            var task = service.RemoveSnapshot(CreateSession(), Node, VmId, "clean-install");

            // Assert
            Assert.Equal(upid, task.Upid);
            Assert.Equal(Node, task.Node);
            mockClient.Verify(c => c.DeleteAsync($"nodes/{Node}/qemu/{VmId}/snapshot/clean-install"), Times.Once);
        }

        [Fact]
        public void RollbackSnapshot_CallsPostAsync_ReturnsUpid()
        {
            // Arrange
            const string upid = "UPID:pve1:000GHI:00000003:5F1234AD:qmrollback:100:root@pam:";
            var json = $@"{{""data"": ""{upid}""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);

            var service = new SnapshotService(mockClient.Object);

            // Act
            var task = service.RollbackSnapshot(CreateSession(), Node, VmId, "clean-install");

            // Assert
            Assert.Equal(upid, task.Upid);
            Assert.Equal(Node, task.Node);
            mockClient.Verify(c => c.PostAsync(
                $"nodes/{Node}/qemu/{VmId}/snapshot/clean-install/rollback",
                It.IsAny<Dictionary<string, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetSnapshots_NullSession_ThrowsArgumentNullException()
        {
            var service = new SnapshotService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.GetSnapshots(null!, Node, VmId));
        }

        [Fact]
        public void CreateSnapshot_NullSession_ThrowsArgumentNullException()
        {
            var service = new SnapshotService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.CreateSnapshot(null!, Node, VmId, "snap"));
        }

        [Fact]
        public void RemoveSnapshot_NullSession_ThrowsArgumentNullException()
        {
            var service = new SnapshotService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.RemoveSnapshot(null!, Node, VmId, "snap"));
        }

        [Fact]
        public void RollbackSnapshot_NullSession_ThrowsArgumentNullException()
        {
            var service = new SnapshotService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.RollbackSnapshot(null!, Node, VmId, "snap"));
        }
    }
}
