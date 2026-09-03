using System;
using System.Collections.Generic;
using System.Linq;
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
    public class VmServiceTests
    {
        private const string TestNode = "pve1";
        private const int TestVmId = 100;

        private static PveSession CreateSession() =>
            new PveSession("pve1.example.com", 8006, true, "PVE:root@pam:TEST_TOKEN");

        [Fact]
        public void ExecuteGuestCommand_SendsCommandAndArgsAsRepeatedCommandArray()
        {
            List<KeyValuePair<string, string>>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Callback<string, IEnumerable<KeyValuePair<string, string>>>((_, data) => captured = data.ToList())
                .ReturnsAsync("{\"data\":{\"pid\":4242}}");

            var service = new VmService(mockClient.Object);
            var pid = service.ExecuteGuestCommand(CreateSession(), TestNode, TestVmId,
                "cmd.exe", new[] { "/c", "echo", "WLMARK42" });

            Assert.Equal(4242, pid);
            Assert.NotNull(captured);

            // Every element (exe + each arg) is its own "command" entry, in order.
            Assert.All(captured!, kvp => Assert.Equal("command", kvp.Key));
            Assert.Equal(
                new[] { "cmd.exe", "/c", "echo", "WLMARK42" },
                captured!.Select(kvp => kvp.Value).ToArray());
        }

        [Fact]
        public void ExecuteGuestCommand_DoesNotUseInputDataForArgs()
        {
            List<KeyValuePair<string, string>>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Callback<string, IEnumerable<KeyValuePair<string, string>>>((_, data) => captured = data.ToList())
                .ReturnsAsync("{\"data\":{\"pid\":1}}");

            var service = new VmService(mockClient.Object);
            service.ExecuteGuestCommand(CreateSession(), TestNode, TestVmId,
                "powershell.exe", new[] { "-NoProfile", "-Command", "echo hi" });

            Assert.NotNull(captured);
            // Args are argv, not STDIN — "input-data" must never be emitted.
            Assert.DoesNotContain(captured!, kvp => kvp.Key == "input-data");
        }

        [Fact]
        public void ExecuteGuestCommand_NoArgs_SendsSingleCommandEntry()
        {
            List<KeyValuePair<string, string>>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Callback<string, IEnumerable<KeyValuePair<string, string>>>((_, data) => captured = data.ToList())
                .ReturnsAsync("{\"data\":{\"pid\":7}}");

            var service = new VmService(mockClient.Object);
            service.ExecuteGuestCommand(CreateSession(), TestNode, TestVmId, "whoami", null);

            Assert.NotNull(captured);
            var only = Assert.Single(captured!);
            Assert.Equal("command", only.Key);
            Assert.Equal("whoami", only.Value);
        }

        [Fact]
        public void ExecuteGuestCommand_EmptyArgs_SendsSingleCommandEntry()
        {
            List<KeyValuePair<string, string>>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Callback<string, IEnumerable<KeyValuePair<string, string>>>((_, data) => captured = data.ToList())
                .ReturnsAsync("{\"data\":{\"pid\":9}}");

            var service = new VmService(mockClient.Object);
            service.ExecuteGuestCommand(CreateSession(), TestNode, TestVmId, "whoami", new string[0]);

            Assert.NotNull(captured);
            var only = Assert.Single(captured!);
            Assert.Equal("command", only.Key);
            Assert.Equal("whoami", only.Value);
        }

        [Fact]
        public void ExecuteGuestCommand_NullArgElement_ThrowsArgumentException()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var service = new VmService(mockClient.Object);

            var ex = Assert.Throws<ArgumentException>(() =>
                service.ExecuteGuestCommand(CreateSession(), TestNode, TestVmId,
                    "cmd.exe", new[] { "/c", null!, "echo" }));
            Assert.Equal("args", ex.ParamName);
        }

        [Fact]
        public void RebootVm_PostsToTheNativeRebootEndpoint()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((r, _) => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:qmreboot:100:root@pam:\"}");

            var service = new VmService(mockClient.Object);
            var task = service.RebootVm(CreateSession(), TestNode, TestVmId);

            // Composing a reboot as shutdown + start races PVE's post-stop cleanup for the
            // config lock; the native endpoint keeps the whole restart server-side.
            Assert.Equal($"nodes/{TestNode}/qemu/{TestVmId}/status/reboot", resource);
            Assert.Contains("qmreboot", task.Upid);
            Assert.Equal("running", task.Status);
        }

        [Fact]
        public void RebootVm_SendsTimeoutWhenSupplied()
        {
            List<KeyValuePair<string, string>>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data.ToList())
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:qmreboot:100:root@pam:\"}");

            var service = new VmService(mockClient.Object);
            service.RebootVm(CreateSession(), TestNode, TestVmId, 45);

            Assert.NotNull(captured);
            Assert.Single(captured!);
            Assert.Equal("timeout", captured![0].Key);
            Assert.Equal("45", captured![0].Value);
        }

        [Fact]
        public void RebootVm_OmitsTimeoutWhenNotSupplied()
        {
            List<KeyValuePair<string, string>>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data.ToList())
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:qmreboot:100:root@pam:\"}");

            var service = new VmService(mockClient.Object);
            service.RebootVm(CreateSession(), TestNode, TestVmId);

            Assert.NotNull(captured);
            Assert.Empty(captured!);
        }

        [Fact]
        public void RemoveVm_WithSkipLockTrue_IncludesSkiplockInQueryString()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .Callback<string>(r => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:qmremove:100:root@pam:\"}");

            var service = new VmService(mockClient.Object);
            service.RemoveVm(CreateSession(), TestNode, TestVmId, purge: false, skipLock: true);

            Assert.NotNull(resource);
            Assert.Contains("skiplock=1", resource!);
        }

        [Fact]
        public void RemoveVm_WithSkipLockFalse_OmitsSkiplockFromQueryString()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .Callback<string>(r => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:qmremove:100:root@pam:\"}");

            var service = new VmService(mockClient.Object);
            service.RemoveVm(CreateSession(), TestNode, TestVmId, purge: false, skipLock: false);

            Assert.NotNull(resource);
            Assert.DoesNotContain("skiplock", resource!);
        }

        [Fact]
        public void RemoveVm_WithPurgeAndSkipLock_IncludesBothInQueryString()
        {
            string? resource = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .Callback<string>(r => resource = r)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:qmremove:100:root@pam:\"}");

            var service = new VmService(mockClient.Object);
            service.RemoveVm(CreateSession(), TestNode, TestVmId, purge: true, skipLock: true);

            Assert.NotNull(resource);
            Assert.Contains("purge=1", resource!);
            Assert.Contains("skiplock=1", resource!);
        }

        [Fact]
        public void CloneVm_WithStorage_IncludesStorageInFormBody()
        {
            Dictionary<string, string>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:qmclone:100:root@pam:\"}");

            var service = new VmService(mockClient.Object);
            service.CloneVm(CreateSession(), TestNode, TestVmId, 200, storage: "local-zfs");

            Assert.NotNull(captured);
            Assert.Equal("local-zfs", captured!["storage"]);
        }

        [Fact]
        public void CloneVm_WithoutStorage_OmitsStorageFromFormBody()
        {
            Dictionary<string, string>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:qmclone:100:root@pam:\"}");

            var service = new VmService(mockClient.Object);
            service.CloneVm(CreateSession(), TestNode, TestVmId, 200);

            Assert.NotNull(captured);
            Assert.False(captured!.ContainsKey("storage"));
        }

        [Fact]
        public void CloneVm_SendsAllocatedNewidNeverZero()
        {
            Dictionary<string, string>? captured = null;
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>>((_, data) => captured = data)
                .ReturnsAsync("{\"data\":\"UPID:pve1:00001234:00005678:6A970AAB:qmclone:100:root@pam:\"}");

            var service = new VmService(mockClient.Object);
            service.CloneVm(CreateSession(), TestNode, TestVmId, 305);

            Assert.NotNull(captured);
            Assert.Equal("305", captured!["newid"]);
        }

        // -----------------------------------------------------------------
        // GetVm
        // -----------------------------------------------------------------

        [Fact]
        public void GetVm_VmNotInNodeListing_ThrowsInvalidOperationException()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync($"nodes/{TestNode}/qemu"))
                .ReturnsAsync("{\"data\":[]}");

            var service = new VmService(mockClient.Object);

            var ex = Assert.Throws<InvalidOperationException>(
                () => service.GetVm(CreateSession(), TestNode, TestVmId));
            Assert.Contains(TestVmId.ToString(), ex.Message);
        }


        // ---------------------------------------------------------------------
        // GetVms multi-node aggregation: issue #142
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
        public void GetVms_AllNodes_A500OnOneNodeIsSkippedAndReportedButOtherNodeResultsReturn()
        {
            var mockClient = SetupTwoNodeCluster();
            mockClient
                .Setup(c => c.GetAsync("nodes/pve1/qemu"))
                .ReturnsAsync("{\"data\":[{\"vmid\":100}]}");
            mockClient
                .Setup(c => c.GetAsync("nodes/pve2/qemu"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.InternalServerError, "internal error", "nodes/pve2/qemu", "GET"));

            var service = new VmService(mockClient.Object);

            var skipped = new List<string>();
            var vms = service.GetVms(CreateSession(), onNodeSkipped: (node, ex) => skipped.Add(node));

            var vm = Assert.Single(vms);
            Assert.Equal(100, vm.VmId);
            Assert.Equal(new[] { "pve2" }, skipped);
        }

        [Fact]
        public void GetVms_AllNodes_A403OnOneNodePropagatesInsteadOfBeingSwallowed()
        {
            var mockClient = SetupTwoNodeCluster();
            mockClient
                .Setup(c => c.GetAsync("nodes/pve1/qemu"))
                .ReturnsAsync("{\"data\":[{\"vmid\":100}]}");
            mockClient
                .Setup(c => c.GetAsync("nodes/pve2/qemu"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.Forbidden, "permission denied", "nodes/pve2/qemu", "GET"));

            var service = new VmService(mockClient.Object);

            var ex = Assert.Throws<PveApiException>(() => service.GetVms(CreateSession()));
            Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        }

        [Fact]
        public void GetVms_AllNodes_ConnectivityFailureOnOneNodeIsSkippedAndReported()
        {
            // PveHttpClient.SendOnceAsync never lets a raw HttpRequestException escape — it
            // wraps one as PveApiException(ServiceUnavailable, ..., inner: HttpRequestException).
            // That is the shape a real connectivity failure takes by the time it reaches
            // VmService, so that is what this test throws.
            var mockClient = SetupTwoNodeCluster();
            mockClient
                .Setup(c => c.GetAsync("nodes/pve1/qemu"))
                .ReturnsAsync("{\"data\":[{\"vmid\":100}]}");
            mockClient
                .Setup(c => c.GetAsync("nodes/pve2/qemu"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.ServiceUnavailable, "connection refused",
                    "nodes/pve2/qemu", "GET", new HttpRequestException("connection refused")));

            var service = new VmService(mockClient.Object);

            var skipped = new List<string>();
            var vms = service.GetVms(CreateSession(), onNodeSkipped: (node, ex) => skipped.Add(node));

            var vm = Assert.Single(vms);
            Assert.Equal(100, vm.VmId);
            Assert.Equal(new[] { "pve2" }, skipped);
        }

        [Fact]
        public void GetVms_AllNodes_ClientTimeoutOnOneNodeIsSkippedAndReported()
        {
            // PveHttpClient.SendOnceAsync wraps an HttpClient timeout as
            // PveApiException(RequestTimeout) — the case of a powered-off or
            // firewall-blackholed node, which must be skipped like any other
            // unreachable node rather than aborting the whole listing.
            var mockClient = SetupTwoNodeCluster();
            mockClient
                .Setup(c => c.GetAsync("nodes/pve1/qemu"))
                .ReturnsAsync("{\"data\":[{\"vmid\":100}]}");
            mockClient
                .Setup(c => c.GetAsync("nodes/pve2/qemu"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.RequestTimeout, "Request timed out after 100s.",
                    "nodes/pve2/qemu", "GET"));

            var service = new VmService(mockClient.Object);

            var skipped = new List<string>();
            var vms = service.GetVms(CreateSession(), onNodeSkipped: (node, ex) => skipped.Add(node));

            var vm = Assert.Single(vms);
            Assert.Equal(100, vm.VmId);
            Assert.Equal(new[] { "pve2" }, skipped);
        }
    }
}
