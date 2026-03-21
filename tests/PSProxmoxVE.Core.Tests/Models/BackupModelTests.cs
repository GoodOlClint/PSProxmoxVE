using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Backup;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class BackupModelTests
    {
        [Fact]
        public void PveBackupJob_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_backup_jobs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var jobs = data.ToObject<PveBackupJob[]>();
            Assert.NotNull(jobs);
            Assert.Equal(2, jobs.Length);
        }

        [Fact]
        public void PveBackupJob_Deserialize_Pve9_FirstJob_HasCorrectId()
        {
            var json = TestHelper.LoadFixture("pve9_backup_jobs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var jobs = data.ToObject<PveBackupJob[]>();
            Assert.NotNull(jobs);
            Assert.Equal("backup-1a2b3c", jobs[0].Id);
        }

        [Fact]
        public void PveBackupJob_Deserialize_Pve9_FirstJob_IsEnabled()
        {
            var json = TestHelper.LoadFixture("pve9_backup_jobs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var jobs = data.ToObject<PveBackupJob[]>();
            Assert.NotNull(jobs);
            Assert.Equal(1, jobs[0].Enabled);
        }

        [Fact]
        public void PveBackupJob_Deserialize_Pve9_FirstJob_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_backup_jobs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var jobs = data.ToObject<PveBackupJob[]>();
            Assert.NotNull(jobs);
            Assert.Equal("vzdump", jobs[0].Type);
            Assert.Equal("0 2 * * *", jobs[0].Schedule);
            Assert.Equal("local", jobs[0].Storage);
            Assert.Equal("snapshot", jobs[0].Mode);
            Assert.Equal("zstd", jobs[0].Compress);
            Assert.Equal("100,101", jobs[0].VmId);
            Assert.Equal("Nightly backup", jobs[0].Comment);
        }

        [Fact]
        public void PveBackupJob_Deserialize_Pve9_FirstJob_AllIsNull()
        {
            var json = TestHelper.LoadFixture("pve9_backup_jobs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var jobs = data.ToObject<PveBackupJob[]>();
            Assert.NotNull(jobs);
            Assert.Null(jobs[0].All);
        }

        [Fact]
        public void PveBackupJob_Deserialize_Pve9_SecondJob_IsDisabled()
        {
            var json = TestHelper.LoadFixture("pve9_backup_jobs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var jobs = data.ToObject<PveBackupJob[]>();
            Assert.NotNull(jobs);
            Assert.Equal(0, jobs[1].Enabled);
        }

        [Fact]
        public void PveBackupJob_Deserialize_Pve9_SecondJob_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_backup_jobs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var jobs = data.ToObject<PveBackupJob[]>();
            Assert.NotNull(jobs);
            Assert.Equal("backup-4d5e6f", jobs[1].Id);
            Assert.Equal("vzdump", jobs[1].Type);
            Assert.Equal("0 4 * * 0", jobs[1].Schedule);
            Assert.Equal("nfs-backup", jobs[1].Storage);
            Assert.Equal("stop", jobs[1].Mode);
            Assert.Equal("Weekly full", jobs[1].Comment);
        }

        [Fact]
        public void PveBackupJob_Deserialize_Pve9_SecondJob_BacksUpAll()
        {
            var json = TestHelper.LoadFixture("pve9_backup_jobs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var jobs = data.ToObject<PveBackupJob[]>();
            Assert.NotNull(jobs);
            Assert.Equal(1, jobs[1].All);
            Assert.Null(jobs[1].VmId);
        }

        [Fact]
        public void PveBackupJob_Deserialize_Pve9_SecondJob_OptionalFieldsAreNull()
        {
            var json = TestHelper.LoadFixture("pve9_backup_jobs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var jobs = data.ToObject<PveBackupJob[]>();
            Assert.NotNull(jobs);
            Assert.Null(jobs[1].Compress);
            Assert.Null(jobs[1].MaxFiles);
            Assert.Null(jobs[1].PruneBackups);
            Assert.Null(jobs[1].MailNotification);
            Assert.Null(jobs[1].MailTo);
            Assert.Null(jobs[1].Node);
            Assert.Null(jobs[1].Exclude);
        }
    }
}
