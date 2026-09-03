using PSProxmoxVE.Core.Utilities;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Utilities
{
    public class PveTaskResponseTests
    {
        [Fact]
        public void Parse_UpidString_ReturnsRunningTaskWithNode()
        {
            var task = PveTaskResponse.Parse(
                "{\"data\":\"UPID:pve1:00001234:00005678:AABBCCDD:qmstart:100:root@pam:\"}",
                "pve1");

            Assert.Equal("UPID:pve1:00001234:00005678:AABBCCDD:qmstart:100:root@pam:", task.Upid);
            Assert.Equal("pve1", task.Node);
            Assert.Equal("running", task.Status);
        }

        [Fact]
        public void Parse_TaskObject_DeserializesAndStampsNode()
        {
            var task = PveTaskResponse.Parse(
                "{\"data\":{\"upid\":\"UPID:pve2:00000001:00000002:00000003:vzdump::root@pam:\",\"status\":\"stopped\",\"exitstatus\":\"OK\",\"node\":\"ignored\"}}",
                "pve2");

            Assert.Equal("UPID:pve2:00000001:00000002:00000003:vzdump::root@pam:", task.Upid);
            Assert.Equal("stopped", task.Status);
            Assert.Equal("OK", task.ExitStatus);
            Assert.Equal("pve2", task.Node);
        }

        [Theory]
        [InlineData("{\"data\":null}")]
        [InlineData("{}")]
        public void Parse_NullOrAbsentData_ReturnsEmptyTaskWithNode(string json)
        {
            var task = PveTaskResponse.Parse(json, "pve3");

            Assert.Equal(string.Empty, task.Upid);
            Assert.Null(task.Status);
            Assert.Equal("pve3", task.Node);
        }
    }
}
