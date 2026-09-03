using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class TaskServiceTests
    {
        private const string TestNode = "pve1";
        private const string TestUpid = "UPID:pve1:000ABC:00000001:5F1234AB:qmstart:100:root@pam:";

        private static PveSession CreateSession()
        {
            return new PveSession("pve1.example.com", 8006, true, "PVE:root@pam:TEST_TOKEN");
        }

        [Fact]
        public void GetTask_HappyPath_ReturnsCorrectFields()
        {
            // Arrange
            var json = @"{
                ""data"": {
                    ""upid"": ""UPID:pve1:000ABC:00000001:5F1234AB:qmstart:100:root@pam:"",
                    ""type"": ""qmstart"",
                    ""status"": ""stopped"",
                    ""exitstatus"": ""OK"",
                    ""node"": ""pve1"",
                    ""starttime"": 1595000000,
                    ""user"": ""root@pam"",
                    ""id"": ""100""
                }
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();

            // Act
            var task = service.GetTask(session, TestNode, TestUpid);

            // Assert
            Assert.Equal(TestUpid, task.Upid);
            Assert.Equal("qmstart", task.Type);
            Assert.Equal("stopped", task.Status);
            Assert.Equal("OK", task.ExitStatus);
            Assert.Equal(TestNode, task.Node);
            Assert.Equal("root@pam", task.User);
            Assert.Equal("100", task.Id);
            Assert.True(task.IsSuccessful);
        }

        [Fact]
        public void GetTaskLog_HappyPath_ReturnsLogEntries()
        {
            // Arrange
            var json = @"{
                ""data"": [
                    { ""n"": 1, ""t"": ""starting task qmstart"" },
                    { ""n"": 2, ""t"": ""VM 100 started"" },
                    { ""n"": 3, ""t"": ""TASK OK"" }
                ]
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();

            // Act
            var logs = service.GetTaskLog(session, TestNode, TestUpid);

            // Assert
            Assert.Equal(3, logs.Length);
            Assert.Equal(1, logs[0].LineNumber);
            Assert.Equal("starting task qmstart", logs[0].Text);
            Assert.Equal("TASK OK", logs[2].Text);
        }

        [Fact]
        public void GetTasks_HappyPath_ReturnsCorrectCount()
        {
            // Arrange
            var json = @"{
                ""data"": [
                    {
                        ""upid"": ""UPID:pve1:000001:00000001:5F1234AB:qmstart:100:root@pam:"",
                        ""type"": ""qmstart"",
                        ""status"": ""stopped"",
                        ""exitstatus"": ""OK"",
                        ""user"": ""root@pam"",
                        ""id"": ""100""
                    },
                    {
                        ""upid"": ""UPID:pve1:000002:00000002:5F1234AC:qmstop:101:root@pam:"",
                        ""type"": ""qmstop"",
                        ""status"": ""running"",
                        ""user"": ""root@pam"",
                        ""id"": ""101""
                    }
                ]
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains("tasks?"))))
                .ReturnsAsync(json);

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();

            // Act
            var tasks = service.GetTasks(session, TestNode, vmid: 100, typeFilter: "qmstart", limit: 10);

            // Assert
            Assert.Equal(2, tasks.Length);
            Assert.Equal("qmstart", tasks[0].Type);
            Assert.Equal(TestNode, tasks[0].Node);
            Assert.Equal(TestNode, tasks[1].Node);
            mockClient.Verify(c => c.GetAsync(It.Is<string>(s =>
                s.Contains("limit=10") &&
                s.Contains("vmid=100") &&
                s.Contains("typefilter=qmstart"))), Times.Once);
        }

        [Fact]
        public void WaitForTask_CompletesWithOK_ReturnsTask()
        {
            // Arrange
            var json = @"{
                ""data"": {
                    ""upid"": ""UPID:pve1:000ABC:00000001:5F1234AB:qmstart:100:root@pam:"",
                    ""type"": ""qmstart"",
                    ""status"": ""stopped"",
                    ""exitstatus"": ""OK"",
                    ""user"": ""root@pam""
                }
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();

            // Act
            var task = service.WaitForTask(session, TestNode, TestUpid,
                timeout: TimeSpan.FromSeconds(5),
                pollInterval: TimeSpan.FromSeconds(1));

            // Assert
            Assert.Equal("stopped", task.Status);
            Assert.Equal("OK", task.ExitStatus);
            Assert.True(task.IsSuccessful);
        }

        [Fact]
        public void WaitForTask_FailedExitStatus_ThrowsPveTaskFailedException()
        {
            // Arrange
            var json = @"{
                ""data"": {
                    ""upid"": ""UPID:pve1:000ABC:00000001:5F1234AB:qmstart:100:root@pam:"",
                    ""type"": ""qmstart"",
                    ""status"": ""stopped"",
                    ""exitstatus"": ""ERROR: VM 100 already running"",
                    ""user"": ""root@pam""
                }
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();

            // Act & Assert
            var ex = Assert.Throws<PveTaskFailedException>(() =>
                service.WaitForTask(session, TestNode, TestUpid,
                    timeout: TimeSpan.FromSeconds(5),
                    pollInterval: TimeSpan.FromSeconds(1)));

            Assert.Equal(TestUpid, ex.Upid);
            Assert.Equal("ERROR: VM 100 already running", ex.ExitStatus);
        }

        [Fact]
        public void WaitForTask_Timeout_ThrowsPveTaskTimeoutException()
        {
            // Arrange — task stays "running" forever
            var json = @"{
                ""data"": {
                    ""upid"": ""UPID:pve1:000ABC:00000001:5F1234AB:qmstart:100:root@pam:"",
                    ""type"": ""qmstart"",
                    ""status"": ""running"",
                    ""user"": ""root@pam""
                }
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();

            // Act & Assert — use very short timeout so the test completes quickly
            var timeout = TimeSpan.FromMilliseconds(100);
            var ex = Assert.Throws<PveTaskTimeoutException>(() =>
                service.WaitForTask(session, TestNode, TestUpid,
                    timeout: timeout,
                    pollInterval: TimeSpan.FromMilliseconds(50)));

            Assert.Equal(TestUpid, ex.Upid);
            Assert.Equal(timeout, ex.Timeout);
        }

        [Fact]
        public void WaitForTask_AlreadyStopped_ChecksStatusBeforeSleeping()
        {
            // Arrange
            var json = @"{
                ""data"": {
                    ""upid"": ""UPID:pve1:000ABC:00000001:5F1234AB:qmstart:100:root@pam:"",
                    ""status"": ""stopped"",
                    ""exitstatus"": ""OK"",
                    ""user"": ""root@pam""
                }
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();

            // Act — a long poll interval would dominate the elapsed time if the
            // implementation slept before its first status check.
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var task = service.WaitForTask(session, TestNode, TestUpid,
                timeout: TimeSpan.FromSeconds(30),
                pollInterval: TimeSpan.FromSeconds(10));
            stopwatch.Stop();

            // Assert
            Assert.True(task.IsSuccessful);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5),
                $"Expected an immediate return for an already-stopped task, took {stopwatch.Elapsed}");
            mockClient.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void WaitForTask_PollIntervalBelowMinimum_IsClampedToOneSecond()
        {
            // Arrange — task reports "running" once, then "stopped".
            var runningJson = @"{ ""data"": { ""status"": ""running"", ""user"": ""root@pam"" } }";
            var stoppedJson = @"{
                ""data"": { ""status"": ""stopped"", ""exitstatus"": ""OK"", ""user"": ""root@pam"" }
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.SetupSequence(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(runningJson)
                .ReturnsAsync(stoppedJson);

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();

            // Act — a zero poll interval must be clamped to the 1-second minimum,
            // not passed through to Thread.Sleep(0).
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var task = service.WaitForTask(session, TestNode, TestUpid,
                timeout: TimeSpan.FromSeconds(30),
                pollInterval: TimeSpan.Zero);
            stopwatch.Stop();

            // Assert
            Assert.True(task.IsSuccessful);
            Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(900),
                $"Expected the clamp to force at least a ~1-second wait, took {stopwatch.Elapsed}");
            mockClient.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void WaitForTask_ProgressCallback_InvokedOnEachPoll()
        {
            // Arrange — two polls report "running", the third reports "stopped".
            var runningJson = @"{ ""data"": { ""status"": ""running"", ""user"": ""root@pam"" } }";
            var stoppedJson = @"{
                ""data"": { ""status"": ""stopped"", ""exitstatus"": ""OK"", ""user"": ""root@pam"" }
            }";

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.SetupSequence(c => c.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(runningJson)
                .ReturnsAsync(runningJson)
                .ReturnsAsync(stoppedJson);

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();
            var seenStatuses = new List<string?>();

            // Act
            var task = service.WaitForTask(session, TestNode, TestUpid,
                timeout: TimeSpan.FromSeconds(30),
                pollInterval: TimeSpan.FromSeconds(1),
                progressCallback: t => seenStatuses.Add(t.Status));

            // Assert
            Assert.True(task.IsSuccessful);
            Assert.Equal(new List<string?> { "running", "running", "stopped" }, seenStatuses);
        }

        private const string RunningJson = @"{ ""data"": { ""status"": ""running"", ""user"": ""root@pam"" } }";
        private const string StoppedJson = @"{ ""data"": { ""status"": ""stopped"", ""exitstatus"": ""OK"", ""user"": ""root@pam"" } }";

        private static (TaskService service, Mock<IPveHttpClient> client, List<TimeSpan> delays) ServiceWithRecordedDelays(int runningPolls)
        {
            var mockClient = new Mock<IPveHttpClient>();
            var sequence = mockClient.SetupSequence(c => c.GetAsync(It.IsAny<string>()));
            for (var i = 0; i < runningPolls; i++)
                sequence = sequence.ReturnsAsync(RunningJson);
            sequence.ReturnsAsync(StoppedJson);

            var delays = new List<TimeSpan>();
            var service = new TaskService(mockClient.Object, d => { delays.Add(d); return Task.CompletedTask; });
            return (service, mockClient, delays);
        }

        [Fact]
        public void WaitForTask_WithNoPollInterval_BacksOffFromOneSecondToATenSecondCap()
        {
            var (service, mockClient, delays) = ServiceWithRecordedDelays(runningPolls: 12);

            var task = service.WaitForTask(CreateSession(), TestNode, TestUpid, timeout: TimeSpan.FromMinutes(5));

            Assert.True(task.IsSuccessful);
            mockClient.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Exactly(13));
            Assert.Equal(12, delays.Count);
            Assert.Equal(TimeSpan.FromSeconds(1), delays[0]);
            Assert.Equal(TimeSpan.FromSeconds(2), delays[1]);
            for (var i = 1; i < delays.Count; i++)
                Assert.True(delays[i] >= delays[i - 1], $"delay {i} ({delays[i]}) shrank from {delays[i - 1]}");
            Assert.All(delays, d => Assert.True(d <= TimeSpan.FromSeconds(10), $"delay {d} exceeds the cap"));
            Assert.Equal(TimeSpan.FromSeconds(10), delays[delays.Count - 1]);
            Assert.Contains(delays, d => d > TimeSpan.FromSeconds(1) && d < TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void WaitForTask_NeverSleepsPastTheDeadline()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(RunningJson);
            var delays = new List<TimeSpan>();
            var service = new TaskService(mockClient.Object, d => { delays.Add(d); return Task.CompletedTask; });
            var timeout = TimeSpan.FromMilliseconds(300);

            Assert.Throws<PveTaskTimeoutException>(() =>
                service.WaitForTask(CreateSession(), TestNode, TestUpid, timeout: timeout));

            Assert.NotEmpty(delays);
            Assert.All(delays, d => Assert.True(d <= timeout, $"slept {d} against a {timeout} timeout"));
        }

        [Fact]
        public void WaitForTask_WithAnExplicitPollInterval_EveryDelayEqualsIt()
        {
            var (service, mockClient, delays) = ServiceWithRecordedDelays(runningPolls: 5);

            service.WaitForTask(CreateSession(), TestNode, TestUpid,
                timeout: TimeSpan.FromMinutes(5), pollInterval: TimeSpan.FromSeconds(3));

            mockClient.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Exactly(6));
            Assert.Equal(5, delays.Count);
            Assert.All(delays, d => Assert.Equal(TimeSpan.FromSeconds(3), d));
        }

        [Fact]
        public void WaitForTask_WithAnExplicitPollIntervalBelowTheMinimum_EveryDelayIsOneSecond()
        {
            var (service, _, delays) = ServiceWithRecordedDelays(runningPolls: 3);

            service.WaitForTask(CreateSession(), TestNode, TestUpid,
                timeout: TimeSpan.FromMinutes(5), pollInterval: TimeSpan.FromMilliseconds(50));

            Assert.Equal(3, delays.Count);
            Assert.All(delays, d => Assert.Equal(TimeSpan.FromSeconds(1), d));
        }

        [Fact]
        public void WaitForTask_PollsTheStatusEndpointOncePerPollThroughTheInjectedClient()
        {
            var (service, mockClient, _) = ServiceWithRecordedDelays(runningPolls: 2);

            service.WaitForTask(CreateSession(), TestNode, TestUpid, timeout: TimeSpan.FromMinutes(5));

            var expected = $"nodes/{TestNode}/tasks/{Uri.EscapeDataString(TestUpid)}/status";
            mockClient.Verify(c => c.GetAsync(expected), Times.Exactly(3));
            mockClient.Verify(c => c.Dispose(), Times.Never);
        }

        private sealed class ClientCountingTaskService : TaskService
        {
            public readonly List<Mock<IPveHttpClient>> Built = new List<Mock<IPveHttpClient>>();
            private readonly int _runningPolls;

            public ClientCountingTaskService(int runningPolls) : base(_ => Task.CompletedTask)
            {
                _runningPolls = runningPolls;
            }

            internal override IPveHttpClient CreateClient(PveSession session, TimeSpan? timeoutOverride)
            {
                var mock = new Mock<IPveHttpClient>();
                var sequence = mock.SetupSequence(c => c.GetAsync(It.IsAny<string>()));
                for (var i = 0; i < _runningPolls; i++)
                    sequence = sequence.ReturnsAsync(RunningJson);
                sequence.ReturnsAsync(StoppedJson);
                Built.Add(mock);
                return mock.Object;
            }
        }

        [Fact]
        public void WaitForTask_WithNoInjectedClient_OpensOneClientForTheWholeWaitAndDisposesItAfter()
        {
            var service = new ClientCountingTaskService(runningPolls: 4);

            var task = service.WaitForTask(CreateSession(), TestNode, TestUpid, timeout: TimeSpan.FromMinutes(5));

            Assert.True(task.IsSuccessful);
            var client = Assert.Single(service.Built);
            client.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Exactly(5));
            client.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public void StopTask_CallsDeleteAsyncWithCorrectPath()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync("{}");

            var service = new TaskService(mockClient.Object);
            var session = CreateSession();

            // Act
            service.StopTask(session, TestNode, TestUpid);

            // Assert
            var encodedUpid = Uri.EscapeDataString(TestUpid);
            mockClient.Verify(c => c.DeleteAsync(
                It.Is<string>(s => s == $"nodes/{TestNode}/tasks/{encodedUpid}")),
                Times.Once);
        }
    }
}
