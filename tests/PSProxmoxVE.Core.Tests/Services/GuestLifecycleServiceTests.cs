using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Moq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Models.Vms;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class GuestLifecycleServiceTests
    {
        private const string TestNode = "pve1";
        private const string TestUpid = "UPID:pve1:000ABC:00000001:5F1234AB:qmstart:100:root@pam:";
        private const int VmId = 100;

        private static PveSession CreateSession() =>
            new PveSession("pve1.example.com", 8006, true, "PVE:root@pam:TEST_TOKEN");

        private static PveTask IssuedTask() => new PveTask { Upid = TestUpid, Status = "running", Node = TestNode };

        private const string TasksStatusFragment = "/tasks/";
        private const string StoppedTaskJson = @"{ ""data"": { ""upid"": """ + TestUpid + @""", ""status"": ""stopped"", ""exitstatus"": ""OK"", ""user"": ""root@pam"" } }";

        /// <summary>Stubs the task-status poll (issued by <see cref="TaskService.WaitForTask"/>) to resolve immediately.</summary>
        private static void StubTaskCompletesImmediately(Mock<IPveHttpClient> mockClient)
        {
            mockClient.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains(TasksStatusFragment))))
                .ReturnsAsync(StoppedTaskJson);
        }

        private static string GuestStatusJson(string status, bool locked = false)
        {
            var lockField = locked ? @",""lock"":""backup""" : "";
            return $@"{{ ""data"": {{ ""status"":""{status}""{lockField} }} }}";
        }

        private static (GuestLifecycleService service, List<TimeSpan> delays) ServiceWithRecordedDelays(Mock<IPveHttpClient> mockClient)
        {
            var delays = new List<TimeSpan>();
            var service = new GuestLifecycleService(mockClient.Object, d => { delays.Add(d); return Task.CompletedTask; });
            return (service, delays);
        }

        [Fact]
        public void WaitForStatusTransition_ReachesExpectedStatusOnSecondPoll_ReturnsTaskAndPollsQemuPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            StubTaskCompletesImmediately(mockClient);
            mockClient.SetupSequence(c => c.GetAsync(It.Is<string>(s => s.Contains("status/current"))))
                .ReturnsAsync(GuestStatusJson("stopped"))
                .ReturnsAsync(GuestStatusJson("running"));

            var (service, delays) = ServiceWithRecordedDelays(mockClient);

            var task = service.WaitForStatusTransition(
                CreateSession(), TestNode, IssuedTask, VmId, "running", timeoutSeconds: 60);

            Assert.Equal(TestUpid, task.Upid);
            var expectedPath = $"nodes/{TestNode}/qemu/{VmId}/status/current";
            mockClient.Verify(c => c.GetAsync(expectedPath), Times.Exactly(2));
            Assert.Single(delays);
        }

        [Fact]
        public void WaitForStatusTransition_ContainerGuest_PollsLxcPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            StubTaskCompletesImmediately(mockClient);
            mockClient.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains("status/current"))))
                .ReturnsAsync(GuestStatusJson("running"));

            var (service, _) = ServiceWithRecordedDelays(mockClient);

            var task = service.WaitForStatusTransition(
                CreateSession(), TestNode, IssuedTask, VmId, "running", timeoutSeconds: 60, isContainer: true);

            Assert.Equal(TestUpid, task.Upid);
            var expectedPath = $"nodes/{TestNode}/lxc/{VmId}/status/current";
            mockClient.Verify(c => c.GetAsync(expectedPath), Times.Once);
        }

        [Fact]
        public void WaitForStatusTransition_StatusMatchedButLocked_KeepsPollingUntilLockClears()
        {
            var mockClient = new Mock<IPveHttpClient>();
            StubTaskCompletesImmediately(mockClient);
            mockClient.SetupSequence(c => c.GetAsync(It.Is<string>(s => s.Contains("status/current"))))
                .ReturnsAsync(GuestStatusJson("running", locked: true))
                .ReturnsAsync(GuestStatusJson("running", locked: true))
                .ReturnsAsync(GuestStatusJson("running", locked: false));

            var (service, delays) = ServiceWithRecordedDelays(mockClient);

            var task = service.WaitForStatusTransition(
                CreateSession(), TestNode, IssuedTask, VmId, "running", timeoutSeconds: 60);

            Assert.Equal(TestUpid, task.Upid);
            var expectedPath = $"nodes/{TestNode}/qemu/{VmId}/status/current";
            mockClient.Verify(c => c.GetAsync(expectedPath), Times.Exactly(3));
            Assert.Equal(2, delays.Count);
        }

        [Fact]
        public void WaitForStatusTransition_MatchedOnFinalPollButStillLocked_ReturnsTaskInsteadOfThrowing()
        {
            // ADR 0015: the guest reports the expected status right up to the deadline; only
            // the lock outlasted the wait, and that must not fail the call.
            var mockClient = new Mock<IPveHttpClient>();
            StubTaskCompletesImmediately(mockClient);
            mockClient.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains("status/current"))))
                .ReturnsAsync(GuestStatusJson("running", locked: true));

            var (service, _) = ServiceWithRecordedDelays(mockClient);

            var task = service.WaitForStatusTransition(
                CreateSession(), TestNode, IssuedTask, VmId, "running", timeoutSeconds: 1);

            Assert.Equal(TestUpid, task.Upid);
        }

        [Fact]
        public void WaitForStatusTransition_NeverMatches_ThrowsPveTaskTimeoutExceptionWithTheUpid()
        {
            var mockClient = new Mock<IPveHttpClient>();
            StubTaskCompletesImmediately(mockClient);
            mockClient.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains("status/current"))))
                .ReturnsAsync(GuestStatusJson("stopped"));

            var (service, _) = ServiceWithRecordedDelays(mockClient);

            var ex = Assert.Throws<PveTaskTimeoutException>(() =>
                service.WaitForStatusTransition(
                    CreateSession(), TestNode, IssuedTask, VmId, "running", timeoutSeconds: 1));

            Assert.Equal(TestUpid, ex.Upid);
        }

        [Fact]
        public void WaitForStatusTransition_MatchedEarlierThenDriftedAway_StillTimesOut()
        {
            // ADR 0015: the fallback tests the most recent observation, not "matched at some
            // point during the wait" — a guest that reached the expected status (here, while
            // still locked, so the wait keeps going) and then drifted away has not satisfied
            // the wait, even though it matched earlier.
            var mockClient = new Mock<IPveHttpClient>();
            StubTaskCompletesImmediately(mockClient);
            var statusPath = $"nodes/{TestNode}/qemu/{VmId}/status/current";
            var pollCount = 0;
            mockClient.Setup(c => c.GetAsync(statusPath))
                .ReturnsAsync(() =>
                {
                    pollCount++;
                    return pollCount <= 2
                        ? GuestStatusJson("running", locked: true)
                        : GuestStatusJson("stopped");
                });

            var (service, _) = ServiceWithRecordedDelays(mockClient);

            var ex = Assert.Throws<PveTaskTimeoutException>(() =>
                service.WaitForStatusTransition(
                    CreateSession(), TestNode, IssuedTask, VmId, "running", timeoutSeconds: 1));

            Assert.Equal(TestUpid, ex.Upid);
            Assert.True(pollCount > 2, $"expected more than two polls before the deadline, got {pollCount}");
        }

        [Fact]
        public void WaitForStatusTransition_A500FromThePoll_IsRetriedAndReportedThroughOnProgress()
        {
            var mockClient = new Mock<IPveHttpClient>();
            StubTaskCompletesImmediately(mockClient);
            var statusPath = $"nodes/{TestNode}/qemu/{VmId}/status/current";
            mockClient.SetupSequence(c => c.GetAsync(statusPath))
                .ThrowsAsync(new PveApiException(HttpStatusCode.InternalServerError, "temporary failure", statusPath, "GET"))
                .ReturnsAsync(GuestStatusJson("running"));

            var (service, _) = ServiceWithRecordedDelays(mockClient);
            var progressMessages = new List<string>();

            var task = service.WaitForStatusTransition(
                CreateSession(), TestNode, IssuedTask, VmId, "running", timeoutSeconds: 60,
                onProgress: progressMessages.Add);

            Assert.Equal(TestUpid, task.Upid);
            Assert.Single(progressMessages);
            Assert.Contains("temporary failure", progressMessages[0]);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.NotFound)]
        public void WaitForStatusTransition_AnAuthOrNotFoundFailure_PropagatesInsteadOfRetrying(HttpStatusCode statusCode)
        {
            var mockClient = new Mock<IPveHttpClient>();
            StubTaskCompletesImmediately(mockClient);
            var statusPath = $"nodes/{TestNode}/qemu/{VmId}/status/current";
            mockClient.Setup(c => c.GetAsync(statusPath))
                .ThrowsAsync(new PveApiException(statusCode, "denied", statusPath, "GET"));

            var (service, _) = ServiceWithRecordedDelays(mockClient);

            var ex = Assert.Throws<PveApiException>(() =>
                service.WaitForStatusTransition(
                    CreateSession(), TestNode, IssuedTask, VmId, "running", timeoutSeconds: 60));

            Assert.Equal(statusCode, ex.StatusCode);
            mockClient.Verify(c => c.GetAsync(statusPath), Times.Once);
        }

        [Fact]
        public void InvokeGuestTask_ReissuesOnGuestLockFailure()
        {
            var mockClient = new Mock<IPveHttpClient>();
            StubTaskCompletesImmediately(mockClient);

            var invocationCount = 0;
            PveTask Issue()
            {
                invocationCount++;
                if (invocationCount == 1)
                    throw new PveApiException(
                        HttpStatusCode.InternalServerError,
                        "can't lock file '/var/lock/qemu-server/lock-100.conf' - got timeout",
                        $"nodes/{TestNode}/qemu/{VmId}/config",
                        "PUT");
                return IssuedTask();
            }

            var service = new GuestLifecycleService(mockClient.Object);
            var progressMessages = new List<string>();

            var task = service.InvokeGuestTask(CreateSession(), TestNode, Issue, onProgress: progressMessages.Add);

            Assert.Equal(TestUpid, task.Upid);
            Assert.Equal(2, invocationCount);
            Assert.Single(progressMessages);
        }

        [Fact]
        public void InvokeGuestTask_TaskFailsOnGuestLock_ReissuesTheWholeOperation()
        {
            // ADR 0020: PVE takes the flock inside the worker for most guest operations, so
            // the failure surfaces as a failed task rather than a failed request and the whole
            // issue-and-wait pair must be reissued, not just the request.
            var mockClient = new Mock<IPveHttpClient>();
            const string lockFailedJson = @"{ ""data"": { ""upid"": """ + TestUpid + @""", ""status"": ""stopped"", "
                + @"""exitstatus"": ""can't lock file '/var/lock/qemu-server/lock-100.conf' - got timeout"", "
                + @"""user"": ""root@pam"" } }";
            mockClient.SetupSequence(c => c.GetAsync(It.Is<string>(s => s.Contains(TasksStatusFragment))))
                .ReturnsAsync(lockFailedJson)
                .ReturnsAsync(StoppedTaskJson);

            var invocationCount = 0;
            PveTask Issue()
            {
                invocationCount++;
                return IssuedTask();
            }

            var service = new GuestLifecycleService(mockClient.Object);
            var progressMessages = new List<string>();

            var task = service.InvokeGuestTask(CreateSession(), TestNode, Issue, onProgress: progressMessages.Add);

            Assert.Equal(TestUpid, task.Upid);
            Assert.Equal(2, invocationCount);
            Assert.Single(progressMessages);
        }

        [Fact]
        public void InvokeGuestTask_EmptyUpid_ReturnsTheIssuedTaskWithoutWaiting()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var issued = new PveTask { Upid = "", Status = "running", Node = TestNode };
            PveTask Issue() => issued;

            var service = new GuestLifecycleService(mockClient.Object);

            var task = service.InvokeGuestTask(CreateSession(), TestNode, Issue);

            Assert.Same(issued, task);
            mockClient.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
