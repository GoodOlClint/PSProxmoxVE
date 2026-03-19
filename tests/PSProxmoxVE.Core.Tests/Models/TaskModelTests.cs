using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Vms;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class TaskModelTests
    {
        [Fact]
        public void PveTask_Deserialize_Completed_HasCorrectUpid()
        {
            var json = TestHelper.LoadFixture("pve9_tasks.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var task = data.ToObject<PveTask>();
            Assert.NotNull(task);
            Assert.Equal("UPID:pve1:00001234:00ABCDEF:65F00000:qmstart:100:root@pam:", task.Upid);
        }

        [Fact]
        public void PveTask_Deserialize_Completed_StatusIsStopped()
        {
            var json = TestHelper.LoadFixture("pve9_tasks.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var task = data.ToObject<PveTask>();
            Assert.NotNull(task);
            Assert.Equal("stopped", task.Status);
        }

        [Fact]
        public void PveTask_Deserialize_Completed_ExitStatusIsOk()
        {
            var json = TestHelper.LoadFixture("pve9_tasks.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var task = data.ToObject<PveTask>();
            Assert.NotNull(task);
            Assert.Equal("OK", task.ExitStatus);
        }

        [Fact]
        public void PveTask_Deserialize_Completed_IsSuccessful_IsTrue()
        {
            var json = TestHelper.LoadFixture("pve9_tasks.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var task = data.ToObject<PveTask>();
            Assert.NotNull(task);
            Assert.True(task.IsSuccessful);
        }

        [Fact]
        public void PveTask_Deserialize_Running_HasCorrectUpid()
        {
            var json = TestHelper.LoadFixture("pve9_task_running.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var task = data.ToObject<PveTask>();
            Assert.NotNull(task);
            Assert.Equal("UPID:pve1:00001234:00ABCDEF:65F00000:qmstart:100:root@pam:", task.Upid);
        }

        [Fact]
        public void PveTask_Deserialize_Running_StatusIsRunning()
        {
            var json = TestHelper.LoadFixture("pve9_task_running.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var task = data.ToObject<PveTask>();
            Assert.NotNull(task);
            Assert.Equal("running", task.Status);
        }

        [Fact]
        public void PveTask_Deserialize_Running_ExitStatus_IsNull()
        {
            var json = TestHelper.LoadFixture("pve9_task_running.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var task = data.ToObject<PveTask>();
            Assert.NotNull(task);
            Assert.Null(task.ExitStatus);
        }

        [Fact]
        public void PveTask_Deserialize_Running_IsSuccessful_IsFalse()
        {
            var json = TestHelper.LoadFixture("pve9_task_running.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var task = data.ToObject<PveTask>();
            Assert.NotNull(task);
            Assert.False(task.IsSuccessful);
        }

        [Fact]
        public void PveTask_IsSuccessful_RequiresBothStoppedAndOk()
        {
            // Stopped but with an error exit status should not be successful
            var task = new PveTask
            {
                Upid = "UPID:pve1:000:000:000:qmstop:100:root@pam:",
                Status = "stopped",
                ExitStatus = "Error: some error occurred"
            };
            Assert.False(task.IsSuccessful);
        }
    }
}
