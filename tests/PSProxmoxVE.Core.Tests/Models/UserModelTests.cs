using Xunit;
using Newtonsoft.Json.Linq;
using PSProxmoxVE.Core.Models.Users;

namespace PSProxmoxVE.Core.Tests.Models
{
    public class UserModelTests
    {
        [Fact]
        public void PveUser_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_users.json");
            var data = JObject.Parse(json)["data"];
            var users = data.ToObject<PveUser[]>();
            Assert.NotNull(users);
            Assert.Equal(3, users.Length);
        }

        [Fact]
        public void PveUser_Deserialize_Pve9_Root_HasCorrectUserId()
        {
            var json = TestHelper.LoadFixture("pve9_users.json");
            var data = JObject.Parse(json)["data"];
            var users = data.ToObject<PveUser[]>();
            Assert.NotNull(users);
            Assert.Equal("root@pam", users[0].UserId);
        }

        [Fact]
        public void PveUser_Deserialize_Pve9_Root_HasCorrectName()
        {
            var json = TestHelper.LoadFixture("pve9_users.json");
            var data = JObject.Parse(json)["data"];
            var users = data.ToObject<PveUser[]>();
            Assert.NotNull(users);
            Assert.Equal("Root", users[0].FirstName);
            Assert.Equal("User", users[0].LastName);
        }

        [Fact]
        public void PveUser_Deserialize_Pve9_Root_HasEmail()
        {
            var json = TestHelper.LoadFixture("pve9_users.json");
            var data = JObject.Parse(json)["data"];
            var users = data.ToObject<PveUser[]>();
            Assert.NotNull(users);
            Assert.Equal("root@example.com", users[0].Email);
        }

        [Fact]
        public void PveUser_Deserialize_Pve9_Root_IsEnabled()
        {
            var json = TestHelper.LoadFixture("pve9_users.json");
            var data = JObject.Parse(json)["data"];
            var users = data.ToObject<PveUser[]>();
            Assert.NotNull(users);
            Assert.Equal(1, users[0].Enabled);
        }

        [Fact]
        public void PveUser_Deserialize_Pve9_Admin_HasGroups()
        {
            var json = TestHelper.LoadFixture("pve9_users.json");
            var data = JObject.Parse(json)["data"];
            var users = data.ToObject<PveUser[]>();
            Assert.NotNull(users);
            Assert.Equal("admin@pve", users[1].UserId);
            Assert.Equal("admins", users[1].Groups);
        }

        [Fact]
        public void PveUser_Deserialize_Pve9_ReadOnly_HasComment()
        {
            var json = TestHelper.LoadFixture("pve9_users.json");
            var data = JObject.Parse(json)["data"];
            var users = data.ToObject<PveUser[]>();
            Assert.NotNull(users);
            Assert.Equal("readonly@pve", users[2].UserId);
            Assert.Equal("Read-only audit user", users[2].Comment);
        }

        [Fact]
        public void PveUser_Deserialize_Pve9_Root_Groups_IsNull()
        {
            var json = TestHelper.LoadFixture("pve9_users.json");
            var data = JObject.Parse(json)["data"];
            var users = data.ToObject<PveUser[]>();
            Assert.NotNull(users);
            // Root user has no groups in fixture
            Assert.Null(users[0].Groups);
        }

        [Fact]
        public void PveRole_Deserialize_Pve9_ReturnsCorrectCount()
        {
            var json = TestHelper.LoadFixture("pve9_roles.json");
            var data = JObject.Parse(json)["data"];
            var roles = data.ToObject<PveRole[]>();
            Assert.NotNull(roles);
            Assert.Equal(2, roles.Length);
        }

        [Fact]
        public void PveRole_Deserialize_Pve9_Administrator_HasCorrectId()
        {
            var json = TestHelper.LoadFixture("pve9_roles.json");
            var data = JObject.Parse(json)["data"];
            var roles = data.ToObject<PveRole[]>();
            Assert.NotNull(roles);
            Assert.Equal("Administrator", roles[0].RoleId);
        }

        [Fact]
        public void PveRole_Deserialize_Pve9_Administrator_IsSpecial()
        {
            var json = TestHelper.LoadFixture("pve9_roles.json");
            var data = JObject.Parse(json)["data"];
            var roles = data.ToObject<PveRole[]>();
            Assert.NotNull(roles);
            Assert.Equal(1, roles[0].Special);
        }

        [Fact]
        public void PveRole_Deserialize_Pve9_Administrator_HasPrivileges()
        {
            var json = TestHelper.LoadFixture("pve9_roles.json");
            var data = JObject.Parse(json)["data"];
            var roles = data.ToObject<PveRole[]>();
            Assert.NotNull(roles);
            Assert.NotNull(roles[0].Privileges);
            Assert.Contains("VM.Allocate", roles[0].Privileges);
        }

        [Fact]
        public void PveRole_Deserialize_Pve9_PVEAuditor_HasLimitedPrivileges()
        {
            var json = TestHelper.LoadFixture("pve9_roles.json");
            var data = JObject.Parse(json)["data"];
            var roles = data.ToObject<PveRole[]>();
            Assert.NotNull(roles);
            Assert.Equal("PVEAuditor", roles[1].RoleId);
            Assert.Contains("VM.Audit", roles[1].Privileges);
        }
    }
}
