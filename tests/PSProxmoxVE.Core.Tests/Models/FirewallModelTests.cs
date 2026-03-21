using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Firewall;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class FirewallModelTests
    {
        // ── PveFirewallRule ──────────────────────────────────────────────

        [Fact]
        public void PveFirewallRule_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_rules.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var rules = data.ToObject<PveFirewallRule[]>();
            Assert.NotNull(rules);
            Assert.Equal(3, rules.Length);
        }

        [Fact]
        public void PveFirewallRule_Deserialize_Pve9_FirstRule_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_rules.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var rules = data.ToObject<PveFirewallRule[]>();
            Assert.NotNull(rules);
            Assert.Equal(0, rules[0].Pos);
            Assert.Equal("in", rules[0].Type);
            Assert.Equal("ACCEPT", rules[0].Action);
            Assert.Equal("tcp", rules[0].Proto);
            Assert.Equal("80", rules[0].Dport);
            Assert.Equal(1, rules[0].Enable);
            Assert.Equal("Allow HTTP", rules[0].Comment);
        }

        [Fact]
        public void PveFirewallRule_Deserialize_Pve9_FirstRule_HasLogLevel()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_rules.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var rules = data.ToObject<PveFirewallRule[]>();
            Assert.NotNull(rules);
            Assert.NotNull(rules[0].Log);
            Assert.Equal("nolog", rules[0].Log);
        }

        [Fact]
        public void PveFirewallRule_Deserialize_Pve9_SecondRule_LogIsNull()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_rules.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var rules = data.ToObject<PveFirewallRule[]>();
            Assert.NotNull(rules);
            Assert.Null(rules[1].Log);
        }

        [Fact]
        public void PveFirewallRule_Deserialize_Pve9_ThirdRule_IsOutboundDrop()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_rules.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var rules = data.ToObject<PveFirewallRule[]>();
            Assert.NotNull(rules);
            Assert.Equal(2, rules[2].Pos);
            Assert.Equal("out", rules[2].Type);
            Assert.Equal("DROP", rules[2].Action);
            Assert.Equal(0, rules[2].Enable);
        }

        [Fact]
        public void PveFirewallRule_Deserialize_Pve9_ThirdRule_NullableFieldsAreNull()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_rules.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var rules = data.ToObject<PveFirewallRule[]>();
            Assert.NotNull(rules);
            Assert.Null(rules[2].Proto);
            Assert.Null(rules[2].Dport);
            Assert.Null(rules[2].Sport);
            Assert.Null(rules[2].Source);
            Assert.Null(rules[2].Dest);
            Assert.Null(rules[2].Macro);
            Assert.Null(rules[2].Iface);
        }

        // ── PveFirewallGroup ────────────────────────────────────────────

        [Fact]
        public void PveFirewallGroup_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_groups.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var groups = data.ToObject<PveFirewallGroup[]>();
            Assert.NotNull(groups);
            Assert.Equal(2, groups.Length);
        }

        [Fact]
        public void PveFirewallGroup_Deserialize_Pve9_FirstGroup_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_groups.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var groups = data.ToObject<PveFirewallGroup[]>();
            Assert.NotNull(groups);
            Assert.Equal("webservers", groups[0].Group);
            Assert.Equal("HTTP/HTTPS servers", groups[0].Comment);
            Assert.Equal("abc123", groups[0].Digest);
        }

        [Fact]
        public void PveFirewallGroup_Deserialize_Pve9_SecondGroup_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_groups.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var groups = data.ToObject<PveFirewallGroup[]>();
            Assert.NotNull(groups);
            Assert.Equal("dbservers", groups[1].Group);
            Assert.Equal("Database servers", groups[1].Comment);
            Assert.Equal("def456", groups[1].Digest);
        }

        // ── PveFirewallAlias ────────────────────────────────────────────

        [Fact]
        public void PveFirewallAlias_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_aliases.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var aliases = data.ToObject<PveFirewallAlias[]>();
            Assert.NotNull(aliases);
            Assert.Equal(2, aliases.Length);
        }

        [Fact]
        public void PveFirewallAlias_Deserialize_Pve9_FirstAlias_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_aliases.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var aliases = data.ToObject<PveFirewallAlias[]>();
            Assert.NotNull(aliases);
            Assert.Equal("local_network", aliases[0].Name);
            Assert.Equal("10.0.0.0/8", aliases[0].Cidr);
            Assert.Equal("RFC1918 private", aliases[0].Comment);
        }

        [Fact]
        public void PveFirewallAlias_Deserialize_Pve9_SecondAlias_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_aliases.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var aliases = data.ToObject<PveFirewallAlias[]>();
            Assert.NotNull(aliases);
            Assert.Equal("dns_server", aliases[1].Name);
            Assert.Equal("8.8.8.8", aliases[1].Cidr);
            Assert.Equal("Google DNS", aliases[1].Comment);
        }

        // ── PveFirewallIpSet ────────────────────────────────────────────

        [Fact]
        public void PveFirewallIpSet_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_ipsets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var ipsets = data.ToObject<PveFirewallIpSet[]>();
            Assert.NotNull(ipsets);
            Assert.Equal(2, ipsets.Length);
        }

        [Fact]
        public void PveFirewallIpSet_Deserialize_Pve9_FirstIpSet_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_ipsets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var ipsets = data.ToObject<PveFirewallIpSet[]>();
            Assert.NotNull(ipsets);
            Assert.Equal("blocklist", ipsets[0].Name);
            Assert.Equal("Blocked IPs", ipsets[0].Comment);
            Assert.Equal("aaa111", ipsets[0].Digest);
        }

        [Fact]
        public void PveFirewallIpSet_Deserialize_Pve9_SecondIpSet_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_ipsets.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var ipsets = data.ToObject<PveFirewallIpSet[]>();
            Assert.NotNull(ipsets);
            Assert.Equal("allowlist", ipsets[1].Name);
            Assert.Equal("Allowed IPs", ipsets[1].Comment);
            Assert.Equal("bbb222", ipsets[1].Digest);
        }

        // ── PveFirewallIpSetEntry ───────────────────────────────────────

        [Fact]
        public void PveFirewallIpSetEntry_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_ipset_entries.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveFirewallIpSetEntry[]>();
            Assert.NotNull(entries);
            Assert.Equal(2, entries.Length);
        }

        [Fact]
        public void PveFirewallIpSetEntry_Deserialize_Pve9_FirstEntry_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_ipset_entries.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveFirewallIpSetEntry[]>();
            Assert.NotNull(entries);
            Assert.Equal("192.168.1.0/24", entries[0].Cidr);
            Assert.Equal(0, entries[0].NoMatch);
            Assert.Equal("Local subnet", entries[0].Comment);
        }

        [Fact]
        public void PveFirewallIpSetEntry_Deserialize_Pve9_SecondEntry_IsExclusion()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_ipset_entries.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var entries = data.ToObject<PveFirewallIpSetEntry[]>();
            Assert.NotNull(entries);
            Assert.Equal("10.0.0.0/8", entries[1].Cidr);
            Assert.Equal(1, entries[1].NoMatch);
            Assert.Equal("Exclude private", entries[1].Comment);
        }

        // ── PveFirewallOptions ──────────────────────────────────────────

        [Fact]
        public void PveFirewallOptions_Deserialize_Pve9_ReturnsNotNull()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_options.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var options = data.ToObject<PveFirewallOptions>();
            Assert.NotNull(options);
        }

        [Fact]
        public void PveFirewallOptions_Deserialize_Pve9_HasCorrectProperties()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_options.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var options = data.ToObject<PveFirewallOptions>();
            Assert.NotNull(options);
            Assert.Equal(1, options.Enable);
            Assert.Equal("DROP", options.PolicyIn);
            Assert.Equal("ACCEPT", options.PolicyOut);
            Assert.Equal("nolog", options.LogLevelIn);
            Assert.Equal("nolog", options.LogLevelOut);
        }

        [Fact]
        public void PveFirewallOptions_Deserialize_Pve9_OptionalFieldsAreNull()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_options.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var options = data.ToObject<PveFirewallOptions>();
            Assert.NotNull(options);
            Assert.Null(options.Dhcp);
            Assert.Null(options.Ndp);
            Assert.Null(options.Radv);
            Assert.Null(options.MacFilter);
            Assert.Null(options.IpFilter);
        }

        // ── PveFirewallRef ──────────────────────────────────────────────

        [Fact]
        public void PveFirewallRef_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_refs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var refs = data.ToObject<PveFirewallRef[]>();
            Assert.NotNull(refs);
            Assert.Equal(2, refs.Length);
        }

        [Fact]
        public void PveFirewallRef_Deserialize_Pve9_FirstRef_IsAlias()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_refs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var refs = data.ToObject<PveFirewallRef[]>();
            Assert.NotNull(refs);
            Assert.Equal("alias", refs[0].Type);
            Assert.Equal("local_network", refs[0].Name);
            Assert.Equal("RFC1918 private", refs[0].Comment);
        }

        [Fact]
        public void PveFirewallRef_Deserialize_Pve9_SecondRef_IsIpSet()
        {
            var json = TestHelper.LoadFixture("pve9_firewall_refs.json");
            var data = JObject.Parse(json)["data"];
            Assert.NotNull(data);
            var refs = data.ToObject<PveFirewallRef[]>();
            Assert.NotNull(refs);
            Assert.Equal("ipset", refs[1].Type);
            Assert.Equal("blocklist", refs[1].Name);
            Assert.Equal("Blocked IPs", refs[1].Comment);
        }
    }
}
