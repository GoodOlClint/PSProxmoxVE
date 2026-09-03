using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Moq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Services;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class ContainerServiceTests
    {
        private const string TestNode = "pve1";
        private const int TestVmId = 100;
        private const string CreateSnapshotUpid = "UPID:pve1:000ABC:00000001:5F1234AB:vzsnapshot:100:root@pam:";

        private static PveSession CreateSession() =>
            new PveSession("pve1.example.com", 8006, true, "PVE:root@pam:TEST_TOKEN");

        private sealed class CapturedPost
        {
            public int Calls { get; set; }
            public string? Path { get; set; }
            public Dictionary<string, string>? Form { get; set; }
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

        // ---------------------------------------------------------------------
        // Snapshots: #126 (Containers area)
        // ---------------------------------------------------------------------

        [Fact]
        public void CreateContainerSnapshot_NameOnly_SendsOnlySnapname()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(CreateSnapshotUpid));
            var service = new ContainerService(mockClient.Object);

            var task = service.CreateContainerSnapshot(CreateSession(), TestNode, TestVmId, "my-snap");

            Assert.Equal(1, captured.Calls);
            Assert.Equal($"nodes/{TestNode}/lxc/{TestVmId}/snapshot", captured.Path);
            Assert.NotNull(captured.Form);
            Assert.Equal("my-snap", captured.Form!["snapname"]);
            Assert.False(captured.Form.ContainsKey("description"));
            Assert.Single(captured.Form);

            Assert.Equal(CreateSnapshotUpid, task.Upid);
            Assert.Equal(TestNode, task.Node);
            Assert.Equal("running", task.Status);
        }

        [Fact]
        public void CreateContainerSnapshot_NullData_ReturnsEmptyUpidWithoutStatus()
        {
            var mockClient = new Mock<IPveHttpClient>();
            CapturePost(mockClient, @"{""data"": null}");
            var service = new ContainerService(mockClient.Object);

            var task = service.CreateContainerSnapshot(CreateSession(), TestNode, TestVmId, "my-snap");

            Assert.Equal(string.Empty, task.Upid);
            Assert.Equal(TestNode, task.Node);
            Assert.Null(task.Status);
        }

        [Fact]
        public void CreateContainerSnapshot_ObjectShapedData_ReturnsTaskFields()
        {
            var json = $@"{{""data"": {{""upid"": ""{CreateSnapshotUpid}"", ""status"": ""stopped"", ""exitstatus"": ""OK""}}}}";
            var mockClient = new Mock<IPveHttpClient>();
            CapturePost(mockClient, json);
            var service = new ContainerService(mockClient.Object);

            var task = service.CreateContainerSnapshot(CreateSession(), TestNode, TestVmId, "my-snap");

            Assert.Equal(CreateSnapshotUpid, task.Upid);
            Assert.Equal(TestNode, task.Node);
            Assert.Equal("stopped", task.Status);
            Assert.Equal("OK", task.ExitStatus);
        }

        [Fact]
        public void CreateContainerSnapshot_WithDescription_SendsDescription()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(CreateSnapshotUpid));
            var service = new ContainerService(mockClient.Object);

            service.CreateContainerSnapshot(CreateSession(), TestNode, TestVmId, "my-snap", "Test snapshot");

            Assert.Equal(1, captured.Calls);
            Assert.NotNull(captured.Form);
            Assert.Equal("my-snap", captured.Form!["snapname"]);
            Assert.Equal("Test snapshot", captured.Form["description"]);
            Assert.Equal(2, captured.Form.Count);
        }

        [Fact]
        public void CreateContainerSnapshot_EscapesNodeInPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(CreateSnapshotUpid));
            var service = new ContainerService(mockClient.Object);

            service.CreateContainerSnapshot(CreateSession(), "pve node", TestVmId, "my-snap");

            Assert.Equal($"nodes/pve%20node/lxc/{TestVmId}/snapshot", captured.Path);
        }

        [Fact]
        public void RemoveContainerSnapshot_CallsDeleteAsync_ReturnsRunningTask()
        {
            const string upid = "UPID:pve1:000DEF:00000002:5F1234AC:vzdelsnap:100:root@pam:";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync(UpidJson(upid));

            var service = new ContainerService(mockClient.Object);

            var task = service.RemoveContainerSnapshot(CreateSession(), TestNode, TestVmId, "clean-install");

            Assert.Equal(upid, task.Upid);
            Assert.Equal(TestNode, task.Node);
            Assert.Equal("running", task.Status);
            mockClient.Verify(c => c.DeleteAsync($"nodes/{TestNode}/lxc/{TestVmId}/snapshot/clean-install"), Times.Once);
            mockClient.VerifyNoOtherCalls();
        }

        [Fact]
        public void RemoveContainerSnapshot_EscapesNodeAndSnapnameInPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync(UpidJson("UPID:pve1:000DEF:00000002:5F1234AC:vzdelsnap:100:root@pam:"));

            var service = new ContainerService(mockClient.Object);

            service.RemoveContainerSnapshot(CreateSession(), "pve node", TestVmId, "snap name");

            mockClient.Verify(c => c.DeleteAsync($"nodes/pve%20node/lxc/{TestVmId}/snapshot/snap%20name"), Times.Once);
        }

        [Fact]
        public void RollbackContainerSnapshot_CallsPostAsync_ReturnsRunningTask()
        {
            const string upid = "UPID:pve1:000GHI:00000003:5F1234AD:vzrollback:100:root@pam:";
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson(upid));

            var service = new ContainerService(mockClient.Object);

            var task = service.RollbackContainerSnapshot(CreateSession(), TestNode, TestVmId, "clean-install");

            Assert.Equal(upid, task.Upid);
            Assert.Equal(TestNode, task.Node);
            Assert.Equal("running", task.Status);
            Assert.Equal($"nodes/{TestNode}/lxc/{TestVmId}/snapshot/clean-install/rollback", captured.Path);
            Assert.Null(captured.Form);
            Assert.Equal(1, captured.Calls);
        }

        [Fact]
        public void RollbackContainerSnapshot_EscapesNodeAndSnapnameInPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, UpidJson("UPID:pve1:000GHI:00000003:5F1234AD:vzrollback:100:root@pam:"));
            var service = new ContainerService(mockClient.Object);

            service.RollbackContainerSnapshot(CreateSession(), "pve node", TestVmId, "snap name");

            Assert.Equal($"nodes/pve%20node/lxc/{TestVmId}/snapshot/snap%20name/rollback", captured.Path);
        }

        [Fact]
        public void CreateContainerSnapshot_NullSession_ThrowsArgumentNullException()
        {
            var service = new ContainerService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session",
                () => service.CreateContainerSnapshot(null!, TestNode, TestVmId, "my-snap"));
        }

        [Fact]
        public void RemoveContainerSnapshot_NullSession_ThrowsArgumentNullException()
        {
            var service = new ContainerService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session",
                () => service.RemoveContainerSnapshot(null!, TestNode, TestVmId, "my-snap"));
        }

        [Fact]
        public void RollbackContainerSnapshot_NullSession_ThrowsArgumentNullException()
        {
            var service = new ContainerService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session",
                () => service.RollbackContainerSnapshot(null!, TestNode, TestVmId, "my-snap"));
        }

        [Fact]
        public void RollbackContainerSnapshot_WhitespaceSnapname_ThrowsArgumentNullException()
        {
            var service = new ContainerService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("snapname",
                () => service.RollbackContainerSnapshot(CreateSession(), TestNode, TestVmId, "  "));
        }

        // ---------------------------------------------------------------------
        // ParseTask "running" stamp: applies to every UPID-string response,
        // not only the three snapshot methods (#126 reconciliation).
        // ---------------------------------------------------------------------

        [Fact]
        public void RemoveContainer_UpidStringResponse_StampsRunningStatus()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzdestroy:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            var task = service.RemoveContainer(CreateSession(), TestNode, TestVmId);

            Assert.Equal("running", task.Status);
        }

        [Fact]
        public void RemoveContainer_WithForceTrue_IncludesForceInQueryString()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .Callback<string>(r => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzdestroy:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.RemoveContainer(CreateSession(), TestNode, TestVmId, purge: false, force: true);

            Assert.Equal($"nodes/{TestNode}/lxc/{TestVmId}?purge=0&force=1", resource);
        }

        [Fact]
        public void RemoveContainer_WithForceFalse_OmitsForceFromQueryString()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .Callback<string>(r => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzdestroy:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.RemoveContainer(CreateSession(), TestNode, TestVmId, purge: false, force: false);

            Assert.Equal($"nodes/{TestNode}/lxc/{TestVmId}?purge=0", resource);
        }

        [Fact]
        public void RemoveContainer_WithPurgeAndForce_IncludesBothInQueryString()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .Callback<string>(r => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzdestroy:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.RemoveContainer(CreateSession(), TestNode, TestVmId, purge: true, force: true);

            Assert.Equal($"nodes/{TestNode}/lxc/{TestVmId}?purge=1&force=1", resource);
        }

        [Fact]
        public void CloneContainer_WithStorage_IncludesStorageInFormBody()
        {
            Dictionary<string, string>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzclone:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.CloneContainer(CreateSession(), TestNode, TestVmId, 200, storage: "local-zfs");

            Assert.NotNull(captured);
            Assert.Equal("local-zfs", captured!["storage"]);
        }

        [Fact]
        public void CloneContainer_WithoutStorage_OmitsStorageFromFormBody()
        {
            Dictionary<string, string>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzclone:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.CloneContainer(CreateSession(), TestNode, TestVmId, 200);

            Assert.NotNull(captured);
            Assert.False(captured!.ContainsKey("storage"));
        }

        [Fact]
        public void CloneContainer_SendsAllocatedNewidNeverZero()
        {
            Dictionary<string, string>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:vzclone:100:root@pam:\"}");

            var service = new ContainerService(mockClient.Object);
            service.CloneContainer(CreateSession(), TestNode, TestVmId, 305);

            Assert.NotNull(captured);
            Assert.Equal("305", captured!["newid"]);
        }

        // ---------------------------------------------------------------------
        // GetContainers multi-node aggregation: issue #142
        // ---------------------------------------------------------------------

        private static Mock<IPveHttpClient> SetupTwoNodeCluster()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync("nodes"))
                .ReturnsAsync("{\"data\":[{\"node\":\"pve1\"},{\"node\":\"pve2\"}]}");
            return mockClient;
        }

        [Fact]
        public void GetContainers_AllNodes_A500OnOneNodeIsSkippedAndReportedButOtherNodeResultsReturn()
        {
            var mockClient = SetupTwoNodeCluster();
            mockClient
                .Setup(c => c.GetAsync("nodes/pve1/lxc"))
                .ReturnsAsync("{\"data\":[{\"vmid\":100}]}");
            mockClient
                .Setup(c => c.GetAsync("nodes/pve2/lxc"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.InternalServerError, "internal error", "nodes/pve2/lxc", "GET"));

            var service = new ContainerService(mockClient.Object);

            var skipped = new List<string>();
            var containers = service.GetContainers(CreateSession(), onNodeSkipped: (node, ex) => skipped.Add(node));

            var ct = Assert.Single(containers);
            Assert.Equal(100, ct.VmId);
            Assert.Equal(new[] { "pve2" }, skipped);
        }

        [Fact]
        public void GetContainers_AllNodes_A403OnOneNodePropagatesInsteadOfBeingSwallowed()
        {
            var mockClient = SetupTwoNodeCluster();
            mockClient
                .Setup(c => c.GetAsync("nodes/pve1/lxc"))
                .ReturnsAsync("{\"data\":[{\"vmid\":100}]}");
            mockClient
                .Setup(c => c.GetAsync("nodes/pve2/lxc"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.Forbidden, "permission denied", "nodes/pve2/lxc", "GET"));

            var service = new ContainerService(mockClient.Object);

            var ex = Assert.Throws<PveApiException>(() => service.GetContainers(CreateSession()));
            Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        }

        [Fact]
        public void GetContainers_AllNodes_ConnectivityFailureOnOneNodeIsSkippedAndReported()
        {
            // PveHttpClient.SendOnceAsync never lets a raw HttpRequestException escape — it
            // wraps one as PveApiException(ServiceUnavailable, ..., inner: HttpRequestException).
            // That is the shape a real connectivity failure takes by the time it reaches
            // ContainerService, so that is what this test throws.
            var mockClient = SetupTwoNodeCluster();
            mockClient
                .Setup(c => c.GetAsync("nodes/pve1/lxc"))
                .ReturnsAsync("{\"data\":[{\"vmid\":100}]}");
            mockClient
                .Setup(c => c.GetAsync("nodes/pve2/lxc"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.ServiceUnavailable, "connection refused",
                    "nodes/pve2/lxc", "GET", new HttpRequestException("connection refused")));

            var service = new ContainerService(mockClient.Object);

            var skipped = new List<string>();
            var containers = service.GetContainers(CreateSession(), onNodeSkipped: (node, ex) => skipped.Add(node));

            var ct = Assert.Single(containers);
            Assert.Equal(100, ct.VmId);
            Assert.Equal(new[] { "pve2" }, skipped);
        }

        [Fact]
        public void GetContainers_AllNodes_ClientTimeoutOnOneNodeIsSkippedAndReported()
        {
            // PveHttpClient.SendOnceAsync wraps an HttpClient timeout as
            // PveApiException(RequestTimeout) — the case of a powered-off or
            // firewall-blackholed node, which must be skipped like any other
            // unreachable node rather than aborting the whole listing.
            var mockClient = SetupTwoNodeCluster();
            mockClient
                .Setup(c => c.GetAsync("nodes/pve1/lxc"))
                .ReturnsAsync("{\"data\":[{\"vmid\":100}]}");
            mockClient
                .Setup(c => c.GetAsync("nodes/pve2/lxc"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.RequestTimeout, "Request timed out after 100s.",
                    "nodes/pve2/lxc", "GET"));

            var service = new ContainerService(mockClient.Object);

            var skipped = new List<string>();
            var containers = service.GetContainers(CreateSession(), onNodeSkipped: (node, ex) => skipped.Add(node));

            var ct = Assert.Single(containers);
            Assert.Equal(100, ct.VmId);
            Assert.Equal(new[] { "pve2" }, skipped);
        }
    }
}
