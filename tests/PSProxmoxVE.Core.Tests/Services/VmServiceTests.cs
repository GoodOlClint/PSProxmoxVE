using System.Collections.Generic;
using System.Linq;
using Moq;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
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
    }
}
