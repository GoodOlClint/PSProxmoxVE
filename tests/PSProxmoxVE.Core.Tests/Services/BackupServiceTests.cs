using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class BackupServiceTests
    {
        private const string Node = "pve1";

        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        // ---------------------------------------------------------------
        // CreateBackup
        // ---------------------------------------------------------------

        [Fact]
        public void CreateBackup_CallsPostAsync_ReturnsUpid()
        {
            // Arrange
            const string upid = "UPID:pve1:000ABC:00000001:5F1234AB:vzdump:100:root@pam:";
            var json = $@"{{""data"": ""{upid}""}}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);

            var config = new Dictionary<string, string>
            {
                ["vmid"] = "100",
                ["storage"] = "local",
                ["mode"] = "snapshot",
                ["compress"] = "zstd"
            };

            var service = new BackupService(mockClient.Object);

            // Act
            var task = service.CreateBackup(CreateSession(), Node, config);

            // Assert
            Assert.Equal(upid, task.Upid);
            Assert.Equal(Node, task.Node);
            mockClient.Verify(c => c.PostAsync(
                $"nodes/{Node}/vzdump",
                It.Is<Dictionary<string, string>>(d => d["vmid"] == "100" && d["storage"] == "local")),
                Times.Once);
        }

        [Fact]
        public void CreateBackup_NullSession_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);
            var config = new Dictionary<string, string> { ["vmid"] = "100" };

            Assert.Throws<ArgumentNullException>("session", () => service.CreateBackup(null!, Node, config));
        }

        [Fact]
        public void CreateBackup_NullConfig_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("config", () => service.CreateBackup(CreateSession(), Node, null!));
        }

        // ---------------------------------------------------------------
        // GetBackupJobs
        // ---------------------------------------------------------------

        [Fact]
        public void GetBackupJobs_ReturnsJobArray()
        {
            // Arrange
            var json = @"{
                ""data"": [
                    {
                        ""id"": ""backup-001"",
                        ""type"": ""vzdump"",
                        ""enabled"": 1,
                        ""schedule"": ""0 2 * * *"",
                        ""storage"": ""pbs-store"",
                        ""mode"": ""snapshot"",
                        ""vmid"": ""100,101,102"",
                        ""compress"": ""zstd""
                    },
                    {
                        ""id"": ""backup-002"",
                        ""type"": ""vzdump"",
                        ""enabled"": 0,
                        ""schedule"": ""0 3 * * 0"",
                        ""storage"": ""local"",
                        ""mode"": ""stop"",
                        ""all"": 1,
                        ""compress"": ""lzo""
                    }
                ]
            }";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/backup"))
                .ReturnsAsync(json);

            var service = new BackupService(mockClient.Object);

            // Act
            var jobs = service.GetBackupJobs(CreateSession());

            // Assert
            Assert.Equal(2, jobs.Length);

            Assert.Equal("backup-001", jobs[0].Id);
            Assert.Equal("vzdump", jobs[0].Type);
            Assert.Equal(1, jobs[0].Enabled);
            Assert.Equal("0 2 * * *", jobs[0].Schedule);
            Assert.Equal("pbs-store", jobs[0].Storage);
            Assert.Equal("snapshot", jobs[0].Mode);
            Assert.Equal("100,101,102", jobs[0].VmId);

            Assert.Equal("backup-002", jobs[1].Id);
            Assert.Equal(0, jobs[1].Enabled);
            Assert.Equal(1, jobs[1].All);
        }

        [Fact]
        public void GetBackupJobs_EmptyData_ReturnsEmptyArray()
        {
            // Arrange
            var json = @"{""data"": []}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/backup"))
                .ReturnsAsync(json);

            var service = new BackupService(mockClient.Object);

            // Act
            var jobs = service.GetBackupJobs(CreateSession());

            // Assert
            Assert.Empty(jobs);
        }

        [Fact]
        public void GetBackupJobs_NullSession_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.GetBackupJobs(null!));
        }

        // ---------------------------------------------------------------
        // GetBackupJob (single)
        // ---------------------------------------------------------------

        [Fact]
        public void GetBackupJob_ReturnsJob()
        {
            // Arrange
            var json = @"{
                ""data"": {
                    ""id"": ""backup-001"",
                    ""type"": ""vzdump"",
                    ""enabled"": 1,
                    ""schedule"": ""0 2 * * *"",
                    ""storage"": ""pbs-store"",
                    ""mode"": ""snapshot"",
                    ""vmid"": ""100""
                }
            }";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/backup/backup-001"))
                .ReturnsAsync(json);

            var service = new BackupService(mockClient.Object);

            // Act
            var job = service.GetBackupJob(CreateSession(), "backup-001");

            // Assert
            Assert.NotNull(job);
            Assert.Equal("backup-001", job!.Id);
            Assert.Equal("snapshot", job.Mode);
            Assert.Equal("pbs-store", job.Storage);
        }

        [Fact]
        public void GetBackupJob_NullSession_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.GetBackupJob(null!, "backup-001"));
        }

        [Fact]
        public void GetBackupJob_NullId_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("id", () => service.GetBackupJob(CreateSession(), null!));
        }

        // ---------------------------------------------------------------
        // CreateBackupJob
        // ---------------------------------------------------------------

        [Fact]
        public void CreateBackupJob_CallsPostAsync()
        {
            // Arrange
            var json = @"{""data"": null}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);

            var config = new Dictionary<string, string>
            {
                ["schedule"] = "0 2 * * *",
                ["storage"] = "local",
                ["mode"] = "snapshot",
                ["vmid"] = "100",
                ["compress"] = "zstd"
            };

            var service = new BackupService(mockClient.Object);

            // Act
            service.CreateBackupJob(CreateSession(), config);

            // Assert
            mockClient.Verify(c => c.PostAsync(
                "cluster/backup",
                It.Is<Dictionary<string, string>>(d =>
                    d["schedule"] == "0 2 * * *" &&
                    d["storage"] == "local")),
                Times.Once);
        }

        [Fact]
        public void CreateBackupJob_NullSession_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);
            var config = new Dictionary<string, string> { ["vmid"] = "100" };

            Assert.Throws<ArgumentNullException>("session", () => service.CreateBackupJob(null!, config));
        }

        [Fact]
        public void CreateBackupJob_NullConfig_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("config", () => service.CreateBackupJob(CreateSession(), null!));
        }

        // ---------------------------------------------------------------
        // UpdateBackupJob
        // ---------------------------------------------------------------

        [Fact]
        public void UpdateBackupJob_CallsPutAsync()
        {
            // Arrange
            var json = @"{""data"": null}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(json);

            var config = new Dictionary<string, string>
            {
                ["schedule"] = "0 4 * * *",
                ["enabled"] = "1"
            };

            var service = new BackupService(mockClient.Object);

            // Act
            service.UpdateBackupJob(CreateSession(), "backup-001", config);

            // Assert
            mockClient.Verify(c => c.PutAsync(
                "cluster/backup/backup-001",
                It.Is<Dictionary<string, string>>(d => d["schedule"] == "0 4 * * *")),
                Times.Once);
        }

        [Fact]
        public void UpdateBackupJob_NullSession_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);
            var config = new Dictionary<string, string> { ["enabled"] = "1" };

            Assert.Throws<ArgumentNullException>("session", () => service.UpdateBackupJob(null!, "id", config));
        }

        [Fact]
        public void UpdateBackupJob_NullId_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);
            var config = new Dictionary<string, string> { ["enabled"] = "1" };

            Assert.Throws<ArgumentNullException>("id", () => service.UpdateBackupJob(CreateSession(), null!, config));
        }

        [Fact]
        public void UpdateBackupJob_NullConfig_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("config", () => service.UpdateBackupJob(CreateSession(), "id", null!));
        }

        // ---------------------------------------------------------------
        // RemoveBackupJob
        // ---------------------------------------------------------------

        [Fact]
        public void RemoveBackupJob_CallsDeleteAsync()
        {
            // Arrange
            var json = @"{""data"": null}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>()))
                .ReturnsAsync(json);

            var service = new BackupService(mockClient.Object);

            // Act
            service.RemoveBackupJob(CreateSession(), "backup-001");

            // Assert
            mockClient.Verify(c => c.DeleteAsync("cluster/backup/backup-001"), Times.Once);
        }

        [Fact]
        public void RemoveBackupJob_NullSession_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.RemoveBackupJob(null!, "id"));
        }

        [Fact]
        public void RemoveBackupJob_NullId_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("id", () => service.RemoveBackupJob(CreateSession(), null!));
        }

        // ---------------------------------------------------------------
        // GetNotBackedUp
        // ---------------------------------------------------------------

        [Fact]
        public void GetNotBackedUp_ReturnsListOfDictionaries()
        {
            // Arrange
            var json = @"{
                ""data"": [
                    { ""vmid"": 100, ""name"": ""webserver"", ""type"": ""qemu"" },
                    { ""vmid"": 200, ""name"": ""database"", ""type"": ""lxc"" }
                ]
            }";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/backup-info/not-backed-up"))
                .ReturnsAsync(json);

            var service = new BackupService(mockClient.Object);

            // Act
            var result = service.GetNotBackedUp(CreateSession());

            // Assert
            Assert.IsType<List<Dictionary<string, object?>>>(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(100L, result[0]["vmid"]);
            Assert.Equal("webserver", result[0]["name"]);
        }

        [Fact]
        public void GetNotBackedUp_EmptyData_ReturnsEmptyList()
        {
            // Arrange
            var json = @"{""data"": []}";
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync("cluster/backup-info/not-backed-up"))
                .ReturnsAsync(json);

            var service = new BackupService(mockClient.Object);

            // Act
            var result = service.GetNotBackedUp(CreateSession());

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetNotBackedUp_NullSession_ThrowsArgumentNullException()
        {
            var service = new BackupService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.GetNotBackedUp(null!));
        }

        // ---------------------------------------------------------------
        // Constructor
        // ---------------------------------------------------------------

        [Fact]
        public void Constructor_NullClient_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("client", () => new BackupService(null!));
        }
    }
}
