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
        public void GetTask_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var mockClient = new Mock<IPveHttpClient>();
            var service = new TaskService(mockClient.Object);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                service.GetTask(null!, TestNode, TestUpid));
            Assert.Equal("session", ex.ParamName);
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
