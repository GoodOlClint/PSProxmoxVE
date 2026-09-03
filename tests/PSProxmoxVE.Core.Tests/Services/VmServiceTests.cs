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
        public void GetGuestExecStatus_ReturnsTypedModelWithUnknownKeyInAdditionalProperties()
        {
            var json = @"{""data"": {
                ""exited"": true,
                ""exitcode"": 0,
                ""out-data"": ""aGVsbG8="",
                ""err-data"": """",
                ""newfield"": ""future""
            }}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync($"nodes/{TestNode}/qemu/{TestVmId}/agent/exec-status?pid=4242"))
                .ReturnsAsync(json);

            var service = new VmService(mockClient.Object);
            var status = service.GetGuestExecStatus(CreateSession(), TestNode, TestVmId, 4242);

            Assert.True(status.Exited);
            Assert.Equal(0, status.ExitCode);
            Assert.Equal("aGVsbG8=", status.OutData);
            Assert.Equal(string.Empty, status.ErrData);
            Assert.Equal("future", status.AdditionalProperties["newfield"]);
        }

        [Fact]
        public void GetGuestExecStatus_ExitedAsString_StillPollsToCompletion()
        {
            // PVE has been observed sending "exited" as the string "1"/"0" as well
            // as a JSON boolean or integer; the poll loop in InvokePveVmGuestExecCmdlet
            // must not throw on this shape.
            var json = @"{""data"": {""exited"": ""1"", ""exitcode"": 0}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync($"nodes/{TestNode}/qemu/{TestVmId}/agent/exec-status?pid=99"))
                .ReturnsAsync(json);

            var service = new VmService(mockClient.Object);
            var status = service.GetGuestExecStatus(CreateSession(), TestNode, TestVmId, 99);

            Assert.True(status.Exited);
            Assert.Equal(0, status.ExitCode);
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
        public void GetVm_HitsStatusCurrentDirectly_NotTheNodeListing()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync($"nodes/{TestNode}/qemu/{TestVmId}/status/current"))
                .ReturnsAsync("{\"data\":{\"vmid\":100,\"name\":\"web1\",\"status\":\"running\"}}");

            var service = new VmService(mockClient.Object);
            var vm = service.GetVm(CreateSession(), TestNode, TestVmId);

            Assert.Equal(100, vm.VmId);
            Assert.Equal("web1", vm.Name);
            Assert.Equal(TestNode, vm.Node);
            mockClient.Verify(c => c.GetAsync($"nodes/{TestNode}/qemu/{TestVmId}/status/current"), Times.Once);
            mockClient.Verify(c => c.GetAsync($"nodes/{TestNode}/qemu"), Times.Never);
        }

        [Fact]
        public void GetVm_NullData_ThrowsInvalidOperationException()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync($"nodes/{TestNode}/qemu/{TestVmId}/status/current"))
                .ReturnsAsync("{}");

            var service = new VmService(mockClient.Object);

            var ex = Assert.Throws<InvalidOperationException>(
                () => service.GetVm(CreateSession(), TestNode, TestVmId));
            Assert.Contains(TestVmId.ToString(), ex.Message);
        }

        [Fact]
        public void GetVm_NotFoundOrServerError_ThrowsInvalidOperationException()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync($"nodes/{TestNode}/qemu/{TestVmId}/status/current"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.InternalServerError, "config file does not exist",
                    $"nodes/{TestNode}/qemu/{TestVmId}/status/current", "GET"));

            var service = new VmService(mockClient.Object);

            var ex = Assert.Throws<InvalidOperationException>(
                () => service.GetVm(CreateSession(), TestNode, TestVmId));
            Assert.Contains(TestVmId.ToString(), ex.Message);
        }

        [Fact]
        public void GetVm_Forbidden_PropagatesInsteadOfBeingSwallowed()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync($"nodes/{TestNode}/qemu/{TestVmId}/status/current"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.Forbidden, "permission denied",
                    $"nodes/{TestNode}/qemu/{TestVmId}/status/current", "GET"));

            var service = new VmService(mockClient.Object);

            var ex = Assert.Throws<PveApiException>(() => service.GetVm(CreateSession(), TestNode, TestVmId));
            Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        }

        [Fact]
        public void GetVm_ConnectivityFailure_PropagatesInsteadOfReadingAsNotFound()
        {
            // PveHttpClient.SendOnceAsync wraps a connectivity failure as
            // PveApiException(ServiceUnavailable) — the node being unreachable, not the VM
            // being absent, so this must not be folded into the not-found conversion.
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync($"nodes/{TestNode}/qemu/{TestVmId}/status/current"))
                .ThrowsAsync(new PveApiException(HttpStatusCode.ServiceUnavailable, "connection refused",
                    $"nodes/{TestNode}/qemu/{TestVmId}/status/current", "GET", new HttpRequestException("connection refused")));

            var service = new VmService(mockClient.Object);

            var ex = Assert.Throws<PveApiException>(() => service.GetVm(CreateSession(), TestNode, TestVmId));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        }

        // ---------------------------------------------------------------------
        // GetVms: issue #152 — all-nodes listing sources from cluster/resources
        // ---------------------------------------------------------------------

        private const string ClusterResourcesJson = @"{
            ""data"": [
                {
                    ""id"": ""qemu/100"",
                    ""type"": ""qemu"",
                    ""vmid"": 100,
                    ""name"": ""web1"",
                    ""status"": ""running"",
                    ""node"": ""pve1"",
                    ""maxcpu"": 2,
                    ""maxmem"": 4294967296,
                    ""maxdisk"": 34359738368,
                    ""uptime"": 3600,
                    ""template"": 0,
                    ""tags"": ""prod;web""
                },
                {
                    ""id"": ""qemu/101"",
                    ""type"": ""qemu"",
                    ""vmid"": 101,
                    ""name"": ""db1"",
                    ""status"": ""stopped"",
                    ""node"": ""pve2"",
                    ""maxcpu"": 4,
                    ""maxmem"": 8589934592,
                    ""maxdisk"": 68719476736,
                    ""uptime"": 0,
                    ""template"": 1
                },
                {
                    ""id"": ""lxc/200"",
                    ""type"": ""lxc"",
                    ""vmid"": 200,
                    ""name"": ""ct1"",
                    ""status"": ""running"",
                    ""node"": ""pve1""
                }
            ]
        }";

        [Fact]
        public void GetVms_NoNode_IssuesExactlyOneClusterResourcesCallAndNoPerNodeCalls()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync("cluster/resources?type=vm"))
                .ReturnsAsync(ClusterResourcesJson);

            var service = new VmService(mockClient.Object);
            var vms = service.GetVms(CreateSession());

            Assert.Equal(2, vms.Length);
            mockClient.Verify(c => c.GetAsync("cluster/resources?type=vm"), Times.Once);
            mockClient.Verify(c => c.GetAsync(It.Is<string>(s => s.Contains("/qemu"))), Times.Never);
            mockClient.Verify(c => c.GetAsync("nodes"), Times.Never);
        }

        [Fact]
        public void GetVms_NoNode_ExcludesLxcRowsFromTheSharedResourcesResponse()
        {
            // "type=vm" is PVE's guest filter, not QEMU-only — it returns lxc rows too.
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync("cluster/resources?type=vm"))
                .ReturnsAsync(ClusterResourcesJson);

            var service = new VmService(mockClient.Object);
            var vms = service.GetVms(CreateSession());

            Assert.DoesNotContain(vms, v => v.VmId == 200);
            Assert.All(vms, v => Assert.NotEqual(200, v.VmId));
        }

        [Fact]
        public void GetVms_NoNode_MapsClusterResourceRowsOntoPveVm()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync("cluster/resources?type=vm"))
                .ReturnsAsync(ClusterResourcesJson);

            var service = new VmService(mockClient.Object);
            var vms = service.GetVms(CreateSession());

            var web1 = vms.Single(v => v.VmId == 100);
            Assert.Equal("web1", web1.Name);
            Assert.Equal("running", web1.Status);
            Assert.Equal("pve1", web1.Node);
            Assert.Equal(2, web1.CpuCount);
            Assert.Equal(4294967296L, web1.MaxMem);
            Assert.Equal(34359738368L, web1.MaxDisk);
            Assert.Equal(3600L, web1.Uptime);
            Assert.Equal(0, web1.Template);
            Assert.Equal("prod;web", web1.Tags);

            var db1 = vms.Single(v => v.VmId == 101);
            Assert.Equal("db1", db1.Name);
            Assert.Equal("stopped", db1.Status);
            Assert.Equal(4, db1.CpuCount);
            Assert.Equal(1, db1.Template);
        }

        [Fact]
        public void GetVms_WithNode_StillHitsNodesQemuNotClusterResources()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient
                .Setup(c => c.GetAsync($"nodes/{TestNode}/qemu"))
                .ReturnsAsync("{\"data\":[{\"vmid\":100}]}");

            var service = new VmService(mockClient.Object);
            var vms = service.GetVms(CreateSession(), TestNode);

            var vm = Assert.Single(vms);
            Assert.Equal(100, vm.VmId);
            mockClient.Verify(c => c.GetAsync($"nodes/{TestNode}/qemu"), Times.Once);
            mockClient.Verify(c => c.GetAsync(It.Is<string>(s => s.StartsWith("cluster/resources"))), Times.Never);
        }
    }
}
