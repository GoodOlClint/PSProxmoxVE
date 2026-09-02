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
        private const string CreateUpid = "UPID:pve1:000ABC:00000001:5F1234AB:qmsnapshot:100:root@pam:";

        private sealed class CapturedPost
        {
            public int Calls { get; set; }
            public string? Path { get; set; }
            public Dictionary<string, string>? Form { get; set; }
        }

        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        private static string UpidJson(string upid) => $@"{{""data"": ""{upid}""}}";

        private static CapturedPost CapturePost(Mock<IPveHttpClient> mockClient, string json)
        {
            var captured = new CapturedPost();
            mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>?>((path, form) =>
                {
                    captured.Calls++;
                    captured.Path = path;
                    captured.Form = form;
                })
                .ReturnsAsync(json);
            return captured;
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
        public void CreateSnapshot_NameOnly_SendsOnlySnapname()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(CreateUpid));
            var service = new SnapshotService(mockClient.Object);

            // Act
            var task = service.CreateSnapshot(CreateSession(), Node, VmId, "my-snap");

            // Assert
            Assert.Equal(1, captured.Calls);
            Assert.Equal($"nodes/{Node}/qemu/{VmId}/snapshot", captured.Path);
            Assert.NotNull(captured.Form);
            Assert.Equal("my-snap", captured.Form!["snapname"]);
            Assert.False(captured.Form.ContainsKey("description"));
            Assert.False(captured.Form.ContainsKey("vmstate"));
            Assert.Single(captured.Form);

            Assert.Equal(CreateUpid, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Equal("running", task.Status);
        }

        [Fact]
        public void CreateSnapshot_WithDescription_SendsDescription()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(CreateUpid));
            var service = new SnapshotService(mockClient.Object);

            // Act
            service.CreateSnapshot(CreateSession(), Node, VmId, "my-snap", "Test snapshot");

            // Assert
            Assert.Equal(1, captured.Calls);
            Assert.NotNull(captured.Form);
            Assert.Equal("my-snap", captured.Form!["snapname"]);
            Assert.Equal("Test snapshot", captured.Form["description"]);
            Assert.False(captured.Form.ContainsKey("vmstate"));
            Assert.Equal(2, captured.Form.Count);
        }

        [Fact]
        public void CreateSnapshot_WithVmState_SendsVmstateOne()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(CreateUpid));
            var service = new SnapshotService(mockClient.Object);

            // Act
            service.CreateSnapshot(CreateSession(), Node, VmId, "my-snap", vmstate: true);

            // Assert
            Assert.Equal(1, captured.Calls);
            Assert.NotNull(captured.Form);
            Assert.Equal("my-snap", captured.Form!["snapname"]);
            Assert.Equal("1", captured.Form["vmstate"]);
            Assert.False(captured.Form.ContainsKey("description"));
            Assert.Equal(2, captured.Form.Count);
        }

        [Fact]
        public void CreateSnapshot_AllFields_SendsExactForm()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(CreateUpid));
            var service = new SnapshotService(mockClient.Object);

            // Act
            var task = service.CreateSnapshot(CreateSession(), Node, VmId, "my-snap", "Test snapshot", vmstate: true);

            // Assert
            Assert.Equal(1, captured.Calls);
            Assert.Equal($"nodes/{Node}/qemu/{VmId}/snapshot", captured.Path);
            Assert.NotNull(captured.Form);
            Assert.Equal("my-snap", captured.Form!["snapname"]);
            Assert.Equal("Test snapshot", captured.Form["description"]);
            Assert.Equal("1", captured.Form["vmstate"]);
            Assert.Equal(3, captured.Form.Count);

            Assert.Equal(CreateUpid, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Equal("running", task.Status);
        }

        [Fact]
        public void CreateSnapshot_EscapesNodeInPath()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(CreateUpid));
            var service = new SnapshotService(mockClient.Object);

            // Act
            service.CreateSnapshot(CreateSession(), "pve node", VmId, "my-snap");

            // Assert
            Assert.Equal($"nodes/pve%20node/qemu/{VmId}/snapshot", captured.Path);
        }

        [Fact]
        public void CreateSnapshot_NullData_ReturnsEmptyUpidWithoutStatus()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            CapturePost(mockClient, @"{""data"": null}");
            var service = new SnapshotService(mockClient.Object);

            // Act
            var task = service.CreateSnapshot(CreateSession(), Node, VmId, "my-snap");

            // Assert
            Assert.Equal(string.Empty, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Null(task.Status);
        }

        [Fact]
        public void CreateSnapshot_ObjectShapedData_ReturnsTaskFields()
        {
            // Arrange
            var json = $@"{{""data"": {{""upid"": ""{CreateUpid}"", ""status"": ""stopped"", ""exitstatus"": ""OK""}}}}";
            var mockClient = new Mock<IPveHttpClient>();
            CapturePost(mockClient, json);
            var service = new SnapshotService(mockClient.Object);

            // Act
            var task = service.CreateSnapshot(CreateSession(), Node, VmId, "my-snap");

            // Assert
            Assert.Equal(CreateUpid, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Equal("stopped", task.Status);
            Assert.Equal("OK", task.ExitStatus);
        }

        [Fact]
        public void RemoveSnapshot_CallsDeleteAsync_ReturnsRunningTask()
        {
            // Arrange
            const string upid = "UPID:pve1:000DEF:00000002:5F1234AC:qmdelsnap:100:root@pam:";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync(UpidJson(upid));

            var service = new SnapshotService(mockClient.Object);

            // Act
            var task = service.RemoveSnapshot(CreateSession(), Node, VmId, "clean-install");

            // Assert
            Assert.Equal(upid, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Equal("running", task.Status);
            mockClient.Verify(c => c.DeleteAsync($"nodes/{Node}/qemu/{VmId}/snapshot/clean-install"), Times.Once);
            mockClient.VerifyNoOtherCalls();
        }

        [Fact]
        public void RemoveSnapshot_EscapesNodeAndSnapnameInPath()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync(UpidJson("UPID:pve1:000DEF:00000002:5F1234AC:qmdelsnap:100:root@pam:"));

            var service = new SnapshotService(mockClient.Object);

            // Act
            service.RemoveSnapshot(CreateSession(), "pve node", VmId, "snap name");

            // Assert
            mockClient.Verify(c => c.DeleteAsync($"nodes/pve%20node/qemu/{VmId}/snapshot/snap%20name"), Times.Once);
        }

        [Fact]
        public void RollbackSnapshot_CallsPostAsync_ReturnsRunningTask()
        {
            // Arrange
            const string upid = "UPID:pve1:000GHI:00000003:5F1234AD:qmrollback:100:root@pam:";
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(upid));

            var service = new SnapshotService(mockClient.Object);

            // Act
            var task = service.RollbackSnapshot(CreateSession(), Node, VmId, "clean-install");

            // Assert
            Assert.Equal(upid, task.Upid);
            Assert.Equal(Node, task.Node);
            Assert.Equal("running", task.Status);
            Assert.Equal($"nodes/{Node}/qemu/{VmId}/snapshot/clean-install/rollback", captured.Path);
            Assert.Null(captured.Form);
            Assert.Equal(1, captured.Calls);
        }

        [Fact]
        public void RollbackSnapshot_EscapesNodeAndSnapnameInPath()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson("UPID:pve1:000GHI:00000003:5F1234AD:qmrollback:100:root@pam:"));
            var service = new SnapshotService(mockClient.Object);

            // Act
            service.RollbackSnapshot(CreateSession(), "pve node", VmId, "snap name");

            // Assert
            Assert.Equal($"nodes/pve%20node/qemu/{VmId}/snapshot/snap%20name/rollback", captured.Path);
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
