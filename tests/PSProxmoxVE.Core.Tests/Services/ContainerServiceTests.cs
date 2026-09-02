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

        private static PveSession CreateSession() =>
            new PveSession("pve1.example.com", 8006, true, "PVE:root@pam:TEST_TOKEN");

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
