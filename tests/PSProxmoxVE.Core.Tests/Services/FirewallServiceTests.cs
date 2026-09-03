using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class FirewallServiceTests
    {
        private const string Group = "web-servers";

        private sealed class CapturedPost
        {
            public int Calls { get; set; }
            public string? Path { get; set; }
            public Dictionary<string, string>? Form { get; set; }
        }

        private sealed class CapturedPut
        {
            public int Calls { get; set; }
            public string? Path { get; set; }
            public Dictionary<string, string>? Form { get; set; }
        }

        private static PveSession CreateSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!testtoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        private static string RulesJson() => @"{
            ""data"": [
                { ""pos"": 0, ""type"": ""in"", ""action"": ""ACCEPT"", ""enable"": 1 },
                { ""pos"": 1, ""type"": ""out"", ""action"": ""DROP"", ""enable"": 0 }
            ]
        }";

        private static CapturedPost CapturePost(Mock<IPveHttpClient> mockClient, string json)
        {
            var captured = new CapturedPost();
            mockClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>?>((path, form) =>
                {
                    captured.Calls++;
                    captured.Path = path;
                    captured.Form = form;
                })
                .ReturnsAsync(json);
            return captured;
        }

        private static CapturedPut CapturePut(Mock<IPveHttpClient> mockClient, string json)
        {
            var captured = new CapturedPut();
            mockClient.Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Callback<string, Dictionary<string, string>?>((path, form) =>
                {
                    captured.Calls++;
                    captured.Path = path;
                    captured.Form = form;
                })
                .ReturnsAsync(json);
            return captured;
        }

        // -------------------------------------------------------------------------
        // GetGroupRules
        // -------------------------------------------------------------------------

        [Fact]
        public void GetGroupRules_ReturnsRuleArray()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(RulesJson());

            var service = new FirewallService(mockClient.Object);

            var rules = service.GetGroupRules(CreateSession(), Group);

            Assert.Equal(2, rules.Length);
            Assert.Equal(0, rules[0].Pos);
            Assert.Equal("in", rules[0].Type);
            Assert.Equal("ACCEPT", rules[0].Action);
            Assert.Equal(1, rules[1].Pos);
            Assert.Equal("out", rules[1].Type);
            Assert.Equal("DROP", rules[1].Action);

            mockClient.Verify(c => c.GetAsync($"cluster/firewall/groups/{Group}"), Times.Once);
        }

        [Fact]
        public void GetGroupRules_NullData_ReturnsEmptyArray()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(@"{""data"": null}");

            var service = new FirewallService(mockClient.Object);

            var rules = service.GetGroupRules(CreateSession(), Group);

            Assert.Empty(rules);
        }

        [Fact]
        public void GetGroupRules_EscapesGroupInPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(RulesJson());

            var service = new FirewallService(mockClient.Object);

            service.GetGroupRules(CreateSession(), "web servers");

            mockClient.Verify(c => c.GetAsync("cluster/firewall/groups/web%20servers"), Times.Once);
        }

        // -------------------------------------------------------------------------
        // CreateGroupRule
        // -------------------------------------------------------------------------

        [Fact]
        public void CreateGroupRule_PostsExactPathAndForm()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, @"{""data"": null}");
            var service = new FirewallService(mockClient.Object);

            var config = new Dictionary<string, string>
            {
                ["type"] = "in",
                ["action"] = "ACCEPT",
                ["enable"] = "1",
                ["source"] = "10.0.0.0/8"
            };

            service.CreateGroupRule(CreateSession(), Group, config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal($"cluster/firewall/groups/{Group}", captured.Path);
            Assert.NotNull(captured.Form);
            Assert.Equal("in", captured.Form!["type"]);
            Assert.Equal("ACCEPT", captured.Form["action"]);
            Assert.Equal("1", captured.Form["enable"]);
            Assert.Equal("10.0.0.0/8", captured.Form["source"]);
            Assert.Equal(4, captured.Form.Count);
        }

        [Fact]
        public void CreateGroupRule_MinimalForm_ForwardsOnlyProvidedKeys()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, @"{""data"": null}");
            var service = new FirewallService(mockClient.Object);

            service.CreateGroupRule(CreateSession(), Group, new Dictionary<string, string>
            {
                ["type"] = "in",
                ["action"] = "ACCEPT"
            });

            Assert.NotNull(captured.Form);
            Assert.False(captured.Form!.ContainsKey("enable"));
            Assert.False(captured.Form.ContainsKey("comment"));
            Assert.Equal(2, captured.Form.Count);
        }

        [Fact]
        public void CreateGroupRule_EscapesGroupInPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePost(mockClient, @"{""data"": null}");
            var service = new FirewallService(mockClient.Object);

            service.CreateGroupRule(CreateSession(), "web servers", new Dictionary<string, string>
            {
                ["type"] = "in",
                ["action"] = "ACCEPT"
            });

            Assert.Equal("cluster/firewall/groups/web%20servers", captured.Path);
        }

        // -------------------------------------------------------------------------
        // UpdateGroupRule
        // -------------------------------------------------------------------------

        [Fact]
        public void UpdateGroupRule_PutsExactPathAndForm()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePut(mockClient, @"{""data"": null}");
            var service = new FirewallService(mockClient.Object);

            var config = new Dictionary<string, string>
            {
                ["action"] = "DROP",
                ["comment"] = "tightened"
            };

            service.UpdateGroupRule(CreateSession(), Group, 2, config);

            Assert.Equal(1, captured.Calls);
            Assert.Equal($"cluster/firewall/groups/{Group}/2", captured.Path);
            Assert.NotNull(captured.Form);
            Assert.Equal("DROP", captured.Form!["action"]);
            Assert.Equal("tightened", captured.Form["comment"]);
            Assert.Equal(2, captured.Form.Count);
        }

        [Fact]
        public void UpdateGroupRule_EscapesGroupInPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            var captured = CapturePut(mockClient, @"{""data"": null}");
            var service = new FirewallService(mockClient.Object);

            service.UpdateGroupRule(CreateSession(), "web servers", 0, new Dictionary<string, string>
            {
                ["action"] = "DROP"
            });

            Assert.Equal("cluster/firewall/groups/web%20servers/0", captured.Path);
        }

        // -------------------------------------------------------------------------
        // RemoveGroupRule
        // -------------------------------------------------------------------------

        [Fact]
        public void RemoveGroupRule_CallsDeleteAsync_ExactPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(@"{""data"": null}");
            var service = new FirewallService(mockClient.Object);

            service.RemoveGroupRule(CreateSession(), Group, 3);

            mockClient.Verify(c => c.DeleteAsync($"cluster/firewall/groups/{Group}/3"), Times.Once);
            mockClient.VerifyNoOtherCalls();
        }

        [Fact]
        public void RemoveGroupRule_EscapesGroupInPath()
        {
            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.DeleteAsync(It.IsAny<string>())).ReturnsAsync(@"{""data"": null}");
            var service = new FirewallService(mockClient.Object);

            service.RemoveGroupRule(CreateSession(), "web servers", 1);

            mockClient.Verify(c => c.DeleteAsync("cluster/firewall/groups/web%20servers/1"), Times.Once);
        }

        // -------------------------------------------------------------------------
        // Guard clauses
        // -------------------------------------------------------------------------

        [Fact]
        public void GetGroupRules_WhitespaceGroup_ThrowsArgumentNullException()
        {
            var service = new FirewallService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("group", () => service.GetGroupRules(CreateSession(), " "));
        }

        // -------------------------------------------------------------------------
        // FirewallScope.TryValidate
        // -------------------------------------------------------------------------

        [Theory]
        [InlineData("Cluster", null, null, null)]
        [InlineData("cluster", null, null, null)]
        [InlineData("Node", "pve1", null, null)]
        [InlineData("NODE", "pve1", null, null)]
        [InlineData("Vm", "pve1", 100, null)]
        [InlineData("vm", "pve1", 100, null)]
        [InlineData("Container", "pve1", 101, null)]
        [InlineData("Group", null, null, "web-servers")]
        [InlineData("GROUP", null, null, "web-servers")]
        public void TryValidate_WithRequiredIdentifiers_ReturnsTrue(string level, string? node, int? vmid, string? group)
        {
            var result = FirewallScope.TryValidate(level, node, vmid, group, out var errorId, out var message);

            Assert.True(result);
            Assert.Equal(string.Empty, errorId);
            Assert.Equal(string.Empty, message);
        }

        [Theory]
        [InlineData("Node")]
        [InlineData("Vm")]
        [InlineData("Container")]
        public void TryValidate_MissingNode_ReturnsFalseWithNodeRequired(string level)
        {
            var result = FirewallScope.TryValidate(level, null, 100, null, out var errorId, out var message);

            Assert.False(result);
            Assert.Equal("NodeRequired", errorId);
            Assert.Equal("Node is required when Level is not Cluster.", message);
        }

        [Fact]
        public void TryValidate_WhitespaceNode_IsAcceptedLikeTheOriginalIsNullOrEmptyCheck()
        {
            var result = FirewallScope.TryValidate("Node", "   ", null, null, out var errorId, out var message);

            Assert.True(result);
            Assert.Equal(string.Empty, errorId);
            Assert.Equal(string.Empty, message);
        }

        [Fact]
        public void TryValidate_VmMissingNodeAndVmId_ReturnsFalseWithNodeRequired()
        {
            var result = FirewallScope.TryValidate("Vm", null, null, null, out var errorId, out var message);

            Assert.False(result);
            Assert.Equal("NodeRequired", errorId);
            Assert.Equal("Node is required when Level is not Cluster.", message);
        }

        [Theory]
        [InlineData("Vm")]
        [InlineData("Container")]
        public void TryValidate_MissingVmId_ReturnsFalseWithVmIdRequired(string level)
        {
            var result = FirewallScope.TryValidate(level, "pve1", null, null, out var errorId, out var message);

            Assert.False(result);
            Assert.Equal("VmIdRequired", errorId);
            Assert.Equal("VmId is required when Level is Vm or Container.", message);
        }

        [Fact]
        public void TryValidate_GroupMissingGroup_ReturnsFalseWithGroupRequired()
        {
            var result = FirewallScope.TryValidate("Group", null, null, null, out var errorId, out var message);

            Assert.False(result);
            Assert.Equal("GroupRequired", errorId);
            Assert.Equal("Group is required when Level is Group.", message);
        }

        [Fact]
        public void TryValidate_GroupWhitespaceGroup_ReturnsFalseWithGroupRequired()
        {
            var result = FirewallScope.TryValidate("Group", null, null, "   ", out var errorId, out var message);

            Assert.False(result);
            Assert.Equal("GroupRequired", errorId);
            Assert.Equal("Group is required when Level is Group.", message);
        }

        // -------------------------------------------------------------------------
        // GetRules base path (cross-checked against an accepted FirewallScope.TryValidate scope)
        // -------------------------------------------------------------------------

        [Theory]
        [InlineData("Cluster", null, null, "cluster/firewall")]
        [InlineData("Node", "pve1", null, "nodes/pve1/firewall")]
        [InlineData("Vm", "pve1", 100, "nodes/pve1/qemu/100/firewall")]
        [InlineData("Container", "pve1", 101, "nodes/pve1/lxc/101/firewall")]
        public void GetRules_AcceptedScope_UsesExpectedBasePath(
            string level, string? node, int? vmid, string expectedPath)
        {
            Assert.True(FirewallScope.TryValidate(level, node, vmid, null, out _, out _));

            var mockClient = new Mock<IPveHttpClient>();
            mockClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(RulesJson());
            var service = new FirewallService(mockClient.Object);

            service.GetRules(CreateSession(), level, node, vmid);

            mockClient.Verify(c => c.GetAsync($"{expectedPath}/rules"), Times.Once);
        }
    }
}
