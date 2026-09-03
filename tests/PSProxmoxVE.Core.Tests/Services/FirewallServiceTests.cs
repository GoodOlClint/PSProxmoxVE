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
        public void GetGroupRules_NullSession_ThrowsArgumentNullException()
        {
            var service = new FirewallService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.GetGroupRules(null!, Group));
        }

        [Fact]
        public void GetGroupRules_WhitespaceGroup_ThrowsArgumentNullException()
        {
            var service = new FirewallService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("group", () => service.GetGroupRules(CreateSession(), " "));
        }

        [Fact]
        public void CreateGroupRule_NullSession_ThrowsArgumentNullException()
        {
            var service = new FirewallService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session",
                () => service.CreateGroupRule(null!, Group, new Dictionary<string, string>()));
        }

        [Fact]
        public void CreateGroupRule_WhitespaceGroup_ThrowsArgumentNullException()
        {
            var service = new FirewallService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("group",
                () => service.CreateGroupRule(CreateSession(), " ", new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateGroupRule_NullSession_ThrowsArgumentNullException()
        {
            var service = new FirewallService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session",
                () => service.UpdateGroupRule(null!, Group, 0, new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateGroupRule_WhitespaceGroup_ThrowsArgumentNullException()
        {
            var service = new FirewallService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("group",
                () => service.UpdateGroupRule(CreateSession(), " ", 0, new Dictionary<string, string>()));
        }

        [Fact]
        public void RemoveGroupRule_NullSession_ThrowsArgumentNullException()
        {
            var service = new FirewallService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("session", () => service.RemoveGroupRule(null!, Group, 0));
        }

        [Fact]
        public void RemoveGroupRule_WhitespaceGroup_ThrowsArgumentNullException()
        {
            var service = new FirewallService(new Mock<IPveHttpClient>().Object);

            Assert.Throws<ArgumentNullException>("group", () => service.RemoveGroupRule(CreateSession(), " ", 0));
        }
    }
}
