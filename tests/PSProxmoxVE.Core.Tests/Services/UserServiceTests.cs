using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Services;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IPveHttpClient> _mockClient;
        private readonly UserService _service;
        private readonly PveSession _session;

        public UserServiceTests()
        {
            _mockClient = new Mock<IPveHttpClient>();
            _service = new UserService(_mockClient.Object);
            _session = new PveSession(
                "pve.example.com",
                8006,
                skipCertificateCheck: true,
                apiToken: "root@pam!test=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        // =================================================================
        // Users
        // =================================================================

        [Fact]
        public void GetUsers_ReturnsUserArray()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("access/users"))
                .ReturnsAsync(@"{""data"":[
                    {""userid"":""root@pam"",""enable"":1,""email"":""root@example.com"",""firstname"":""Root"",""lastname"":""Admin""},
                    {""userid"":""deploy@pve"",""enable"":1,""groups"":""admins"",""comment"":""Deployment account""}
                ]}");

            // Act
            var result = _service.GetUsers(_session);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal("root@pam", result[0].UserId);
            Assert.Equal("root@example.com", result[0].Email);
            Assert.Equal("Root", result[0].FirstName);
            Assert.Equal("deploy@pve", result[1].UserId);
            Assert.Equal("admins", result[1].Groups);
            _mockClient.Verify(c => c.GetAsync("access/users"), Times.Once);
        }

        [Fact]
        public void GetUsers_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetUsers(null!));
        }

        [Fact]
        public void GetUser_ReturnsSingleUser()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("access/users/root%40pam"))
                .ReturnsAsync(@"{""data"":{""email"":""root@example.com"",""firstname"":""Root"",""lastname"":""Admin"",""enable"":1}}");

            // Act
            var result = _service.GetUser(_session, "root@pam");

            // Assert
            Assert.Equal("root@pam", result.UserId);
            Assert.Equal("root@example.com", result.Email);
            Assert.Equal(1, result.Enabled);
        }

        [Fact]
        public void GetUser_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetUser(null!, "root@pam"));
        }

        [Fact]
        public void GetUser_NullUserId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetUser(_session, null!));
        }

        [Fact]
        public void CreateUser_PostsFormData()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                ["email"] = "newuser@example.com",
                ["firstname"] = "New",
                ["lastname"] = "User"
            };
            _mockClient.Setup(c => c.PostAsync("access/users", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.CreateUser(_session, "newuser@pve", config);

            // Assert
            _mockClient.Verify(c => c.PostAsync("access/users",
                It.Is<Dictionary<string, string>>(d =>
                    d["userid"] == "newuser@pve" &&
                    d["email"] == "newuser@example.com")),
                Times.Once);
        }

        [Fact]
        public void CreateUser_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateUser(null!, "user@pve"));
        }

        [Fact]
        public void CreateUser_NullUserId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateUser(_session, null!));
        }

        [Fact]
        public void SetUser_PutsFormData()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                ["email"] = "updated@example.com",
                ["comment"] = "Updated account"
            };
            _mockClient.Setup(c => c.PutAsync("access/users/deploy%40pve", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.SetUser(_session, "deploy@pve", config);

            // Assert
            _mockClient.Verify(c => c.PutAsync("access/users/deploy%40pve",
                It.Is<Dictionary<string, string>>(d =>
                    d["email"] == "updated@example.com" &&
                    d["comment"] == "Updated account")),
                Times.Once);
        }

        [Fact]
        public void SetUser_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.SetUser(null!, "user@pve", new Dictionary<string, object>()));
        }

        [Fact]
        public void SetUser_NullUserId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.SetUser(_session, null!, new Dictionary<string, object>()));
        }

        [Fact]
        public void SetUser_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.SetUser(_session, "user@pve", null!));
        }

        [Fact]
        public void RemoveUser_CallsDeleteWithCorrectPath()
        {
            // Arrange
            _mockClient.Setup(c => c.DeleteAsync("access/users/deploy%40pve"))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.RemoveUser(_session, "deploy@pve");

            // Assert
            _mockClient.Verify(c => c.DeleteAsync("access/users/deploy%40pve"), Times.Once);
        }

        [Fact]
        public void RemoveUser_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveUser(null!, "user@pve"));
        }

        [Fact]
        public void RemoveUser_NullUserId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveUser(_session, null!));
        }

        // =================================================================
        // API Tokens
        // =================================================================

        [Fact]
        public void GetApiTokens_ReturnsTokenArray()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("access/users/root%40pam/token"))
                .ReturnsAsync(@"{""data"":[
                    {""tokenid"":""automation"",""privsep"":1,""expire"":0,""comment"":""CI/CD token""},
                    {""tokenid"":""monitoring"",""privsep"":0,""expire"":1735689600}
                ]}");

            // Act
            var result = _service.GetApiTokens(_session, "root@pam");

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal("automation", result[0].TokenId);
            Assert.Equal("root@pam", result[0].UserId);
            Assert.Equal(1, result[0].PrivilegeSeparation);
            Assert.Equal("monitoring", result[1].TokenId);
            Assert.Equal("root@pam", result[1].UserId);
        }

        [Fact]
        public void GetApiTokens_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetApiTokens(null!, "root@pam"));
        }

        [Fact]
        public void GetApiTokens_NullUserId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetApiTokens(_session, null!));
        }

        [Fact]
        public void CreateApiToken_ReturnsTokenWithSecret()
        {
            // Arrange
            _mockClient.Setup(c => c.PostAsync(
                    "access/users/root%40pam/token/deploy",
                    It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":{""full-tokenid"":""root@pam!deploy"",""value"":""aabbccdd-1122-3344-5566-778899aabbcc"",""info"":{""privsep"":1,""expire"":0}}}");

            // Act
            var result = _service.CreateApiToken(_session, "root@pam", "deploy", comment: "Deploy token", privilegeSeparation: true);

            // Assert
            Assert.Equal("root@pam", result.UserId);
            Assert.Equal("deploy", result.TokenId);
            Assert.Equal("aabbccdd-1122-3344-5566-778899aabbcc", result.Value);
        }

        [Fact]
        public void CreateApiToken_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.CreateApiToken(null!, "root@pam", "token1"));
        }

        [Fact]
        public void CreateApiToken_NullUserId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.CreateApiToken(_session, null!, "token1"));
        }

        [Fact]
        public void CreateApiToken_NullTokenId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.CreateApiToken(_session, "root@pam", null!));
        }

        [Fact]
        public void RemoveApiToken_CallsDeleteWithCorrectPath()
        {
            // Arrange
            _mockClient.Setup(c => c.DeleteAsync("access/users/root%40pam/token/automation"))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.RemoveApiToken(_session, "root@pam", "automation");

            // Assert
            _mockClient.Verify(c => c.DeleteAsync("access/users/root%40pam/token/automation"), Times.Once);
        }

        [Fact]
        public void RemoveApiToken_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.RemoveApiToken(null!, "root@pam", "token1"));
        }

        [Fact]
        public void RemoveApiToken_NullUserId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.RemoveApiToken(_session, null!, "token1"));
        }

        [Fact]
        public void RemoveApiToken_NullTokenId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.RemoveApiToken(_session, "root@pam", null!));
        }

        [Fact]
        public void UpdateApiToken_CallsPutWithCorrectPath()
        {
            // Arrange
            var config = new Dictionary<string, string> { ["comment"] = "Updated comment" };
            _mockClient.Setup(c => c.PutAsync("access/users/root%40pam/token/automation", config))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.UpdateApiToken(_session, "root@pam", "automation", config);

            // Assert
            _mockClient.Verify(c => c.PutAsync("access/users/root%40pam/token/automation", config), Times.Once);
        }

        [Fact]
        public void UpdateApiToken_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateApiToken(null!, "root@pam", "token1", new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateApiToken_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateApiToken(_session, "root@pam", "token1", null!));
        }

        // =================================================================
        // Roles
        // =================================================================

        [Fact]
        public void GetRoles_ReturnsRoleArray()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("access/roles"))
                .ReturnsAsync(@"{""data"":[
                    {""roleid"":""Administrator"",""privs"":""Datastore.Allocate,Datastore.AllocateSpace,Datastore.Audit"",""special"":1},
                    {""roleid"":""PVEVMAdmin"",""privs"":""VM.Allocate,VM.Config.Disk,VM.Config.CPU"",""special"":1},
                    {""roleid"":""CustomOps"",""privs"":""VM.PowerMgmt,VM.Console"",""special"":0}
                ]}");

            // Act
            var result = _service.GetRoles(_session);

            // Assert
            Assert.Equal(3, result.Length);
            Assert.Equal("Administrator", result[0].RoleId);
            Assert.Equal(1, result[0].Special);
            Assert.Equal("CustomOps", result[2].RoleId);
            Assert.Equal(0, result[2].Special);
        }

        [Fact]
        public void GetRoles_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetRoles(null!));
        }

        [Fact]
        public void CreateRole_PostsFormData()
        {
            // Arrange
            _mockClient.Setup(c => c.PostAsync("access/roles", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.CreateRole(_session, "BackupOperator", "Datastore.Audit,Datastore.AllocateSpace");

            // Assert
            _mockClient.Verify(c => c.PostAsync("access/roles",
                It.Is<Dictionary<string, string>>(d =>
                    d["roleid"] == "BackupOperator" &&
                    d["privs"] == "Datastore.Audit,Datastore.AllocateSpace")),
                Times.Once);
        }

        [Fact]
        public void CreateRole_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateRole(null!, "role1"));
        }

        [Fact]
        public void CreateRole_NullRoleId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateRole(_session, null!));
        }

        [Fact]
        public void UpdateRole_PutsPrivileges()
        {
            // Arrange
            _mockClient.Setup(c => c.PutAsync("access/roles/BackupOperator", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.UpdateRole(_session, "BackupOperator", "Datastore.Audit,Datastore.AllocateSpace,Datastore.Allocate");

            // Assert
            _mockClient.Verify(c => c.PutAsync("access/roles/BackupOperator",
                It.Is<Dictionary<string, string>>(d =>
                    d["privs"] == "Datastore.Audit,Datastore.AllocateSpace,Datastore.Allocate")),
                Times.Once);
        }

        [Fact]
        public void UpdateRole_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateRole(null!, "role1", "privs"));
        }

        [Fact]
        public void UpdateRole_NullRoleId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateRole(_session, null!, "privs"));
        }

        [Fact]
        public void UpdateRole_NullPrivileges_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateRole(_session, "role1", null!));
        }

        [Fact]
        public void RemoveRole_CallsDeleteWithCorrectPath()
        {
            // Arrange
            _mockClient.Setup(c => c.DeleteAsync("access/roles/BackupOperator"))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.RemoveRole(_session, "BackupOperator");

            // Assert
            _mockClient.Verify(c => c.DeleteAsync("access/roles/BackupOperator"), Times.Once);
        }

        [Fact]
        public void RemoveRole_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveRole(null!, "role1"));
        }

        [Fact]
        public void RemoveRole_NullRoleId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveRole(_session, null!));
        }

        // =================================================================
        // Groups
        // =================================================================

        [Fact]
        public void GetGroups_ReturnsGroupArray()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("access/groups"))
                .ReturnsAsync(@"{""data"":[
                    {""groupid"":""admins"",""comment"":""System administrators"",""users"":""root@pam,admin@pve""},
                    {""groupid"":""operators"",""comment"":""Operations team""}
                ]}");

            // Act
            var result = _service.GetGroups(_session);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal("admins", result[0].GroupId);
            Assert.Equal("System administrators", result[0].Comment);
            Assert.Equal("root@pam,admin@pve", result[0].Users);
            Assert.Equal("operators", result[1].GroupId);
        }

        [Fact]
        public void GetGroups_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetGroups(null!));
        }

        [Fact]
        public void CreateGroup_PostsFormData()
        {
            // Arrange
            _mockClient.Setup(c => c.PostAsync("access/groups", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.CreateGroup(_session, "devops", "DevOps engineering team");

            // Assert
            _mockClient.Verify(c => c.PostAsync("access/groups",
                It.Is<Dictionary<string, string>>(d =>
                    d["groupid"] == "devops" &&
                    d["comment"] == "DevOps engineering team")),
                Times.Once);
        }

        [Fact]
        public void CreateGroup_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateGroup(null!, "group1"));
        }

        [Fact]
        public void CreateGroup_NullGroupId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.CreateGroup(_session, null!));
        }

        [Fact]
        public void UpdateGroup_PutsConfig()
        {
            // Arrange
            var config = new Dictionary<string, string> { ["comment"] = "Updated description" };
            _mockClient.Setup(c => c.PutAsync("access/groups/devops", config))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.UpdateGroup(_session, "devops", config);

            // Assert
            _mockClient.Verify(c => c.PutAsync("access/groups/devops", config), Times.Once);
        }

        [Fact]
        public void UpdateGroup_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateGroup(null!, "group1", new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateGroup_NullGroupId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateGroup(_session, null!, new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateGroup_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateGroup(_session, "group1", null!));
        }

        [Fact]
        public void RemoveGroup_CallsDeleteWithCorrectPath()
        {
            // Arrange
            _mockClient.Setup(c => c.DeleteAsync("access/groups/devops"))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.RemoveGroup(_session, "devops");

            // Assert
            _mockClient.Verify(c => c.DeleteAsync("access/groups/devops"), Times.Once);
        }

        [Fact]
        public void RemoveGroup_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveGroup(null!, "group1"));
        }

        [Fact]
        public void RemoveGroup_NullGroupId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveGroup(_session, null!));
        }

        // =================================================================
        // Domains / Realms
        // =================================================================

        [Fact]
        public void GetDomains_ReturnsDomainArray()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("access/domains"))
                .ReturnsAsync(@"{""data"":[
                    {""realm"":""pam"",""type"":""pam"",""comment"":""Linux PAM standard authentication"",""default"":0},
                    {""realm"":""pve"",""type"":""pve"",""comment"":""Proxmox VE authentication server"",""default"":1},
                    {""realm"":""corp-ldap"",""type"":""ldap"",""comment"":""Corporate LDAP""}
                ]}");

            // Act
            var result = _service.GetDomains(_session);

            // Assert
            Assert.Equal(3, result.Length);
            Assert.Equal("pam", result[0].Realm);
            Assert.Equal("pam", result[0].Type);
            Assert.Equal("pve", result[1].Realm);
            Assert.Equal(1, result[1].Default);
            Assert.Equal("corp-ldap", result[2].Realm);
            Assert.Equal("ldap", result[2].Type);
        }

        [Fact]
        public void GetDomains_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetDomains(null!));
        }

        [Fact]
        public void CreateDomain_PostsConfig()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                ["realm"] = "corp-ad",
                ["type"] = "ad",
                ["server1"] = "dc01.corp.local",
                ["domain"] = "corp.local"
            };
            _mockClient.Setup(c => c.PostAsync("access/domains", config))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.CreateDomain(_session, config);

            // Assert
            _mockClient.Verify(c => c.PostAsync("access/domains", config), Times.Once);
        }

        [Fact]
        public void CreateDomain_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.CreateDomain(null!, new Dictionary<string, string>()));
        }

        [Fact]
        public void CreateDomain_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.CreateDomain(_session, null!));
        }

        [Fact]
        public void UpdateDomain_PutsConfig()
        {
            // Arrange
            var config = new Dictionary<string, string> { ["comment"] = "Updated AD realm" };
            _mockClient.Setup(c => c.PutAsync("access/domains/corp-ad", config))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.UpdateDomain(_session, "corp-ad", config);

            // Assert
            _mockClient.Verify(c => c.PutAsync("access/domains/corp-ad", config), Times.Once);
        }

        [Fact]
        public void UpdateDomain_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateDomain(null!, "pam", new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateDomain_NullRealm_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateDomain(_session, null!, new Dictionary<string, string>()));
        }

        [Fact]
        public void UpdateDomain_NullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.UpdateDomain(_session, "pam", null!));
        }

        [Fact]
        public void RemoveDomain_CallsDeleteWithCorrectPath()
        {
            // Arrange
            _mockClient.Setup(c => c.DeleteAsync("access/domains/corp-ad"))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.RemoveDomain(_session, "corp-ad");

            // Assert
            _mockClient.Verify(c => c.DeleteAsync("access/domains/corp-ad"), Times.Once);
        }

        [Fact]
        public void RemoveDomain_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveDomain(null!, "pam"));
        }

        [Fact]
        public void RemoveDomain_NullRealm_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.RemoveDomain(_session, null!));
        }

        // =================================================================
        // Password
        // =================================================================

        [Fact]
        public void ChangePassword_PutsCredentials()
        {
            // Arrange
            _mockClient.Setup(c => c.PutAsync("access/password", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.ChangePassword(_session, "deploy@pve", "newSecureP@ss!");

            // Assert
            _mockClient.Verify(c => c.PutAsync("access/password",
                It.Is<Dictionary<string, string>>(d =>
                    d["userid"] == "deploy@pve" &&
                    d["password"] == "newSecureP@ss!")),
                Times.Once);
        }

        [Fact]
        public void ChangePassword_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.ChangePassword(null!, "user@pve", "pass"));
        }

        [Fact]
        public void ChangePassword_NullUserId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.ChangePassword(_session, null!, "pass"));
        }

        [Fact]
        public void ChangePassword_NullPassword_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.ChangePassword(_session, "user@pve", null!));
        }

        // =================================================================
        // Permissions / ACLs
        // =================================================================

        [Fact]
        public void GetPermissions_ReturnsPermissionsFromObjectResponse()
        {
            // Arrange — the PVE API returns permissions as a path-keyed object
            _mockClient.Setup(c => c.GetAsync("access/permissions"))
                .ReturnsAsync(@"{""data"":{
                    ""/"":{ ""Datastore.Audit"":1, ""VM.Audit"":1 },
                    ""/nodes/pve1"":{ ""Sys.Console"":1 }
                }}");

            // Act
            var result = _service.GetPermissions(_session);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Contains(result, p => p.Path == "/");
            Assert.Contains(result, p => p.Path == "/nodes/pve1");
        }

        [Fact]
        public void GetPermissions_WithUserIdFilter_AppendsQueryParam()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("access/permissions?userid=deploy%40pve"))
                .ReturnsAsync(@"{""data"":{""/"":{}}}");

            // Act
            var result = _service.GetPermissions(_session, userId: "deploy@pve");

            // Assert
            _mockClient.Verify(c => c.GetAsync("access/permissions?userid=deploy%40pve"), Times.Once);
            Assert.Single(result);
        }

        [Fact]
        public void GetPermissions_WithPathFilter_AppendsQueryParam()
        {
            // Arrange
            _mockClient.Setup(c => c.GetAsync("access/permissions?path=%2Fvms%2F100"))
                .ReturnsAsync(@"{""data"":{}}");

            // Act
            var result = _service.GetPermissions(_session, path: "/vms/100");

            // Assert
            _mockClient.Verify(c => c.GetAsync("access/permissions?path=%2Fvms%2F100"), Times.Once);
            Assert.Empty(result);
        }

        [Fact]
        public void GetPermissions_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetPermissions(null!));
        }

        [Fact]
        public void SetPermission_PutsAclData()
        {
            // Arrange
            _mockClient.Setup(c => c.PutAsync("access/acl", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.SetPermission(_session, "/vms/100", "PVEVMAdmin", users: "deploy@pve", propagate: true);

            // Assert
            _mockClient.Verify(c => c.PutAsync("access/acl",
                It.Is<Dictionary<string, string>>(d =>
                    d["path"] == "/vms/100" &&
                    d["roles"] == "PVEVMAdmin" &&
                    d["users"] == "deploy@pve" &&
                    d["propagate"] == "1" &&
                    d["delete"] == "0")),
                Times.Once);
        }

        [Fact]
        public void SetPermission_WithGroupAndDelete_PutsCorrectFlags()
        {
            // Arrange
            _mockClient.Setup(c => c.PutAsync("access/acl", It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(@"{""data"":null}");

            // Act
            _service.SetPermission(_session, "/", "Administrator", groups: "admins", propagate: false, delete: true);

            // Assert
            _mockClient.Verify(c => c.PutAsync("access/acl",
                It.Is<Dictionary<string, string>>(d =>
                    d["path"] == "/" &&
                    d["roles"] == "Administrator" &&
                    d["groups"] == "admins" &&
                    d["propagate"] == "0" &&
                    d["delete"] == "1")),
                Times.Once);
        }

        [Fact]
        public void SetPermission_NullSession_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.SetPermission(null!, "/", "Admin"));
        }

        [Fact]
        public void SetPermission_NullPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.SetPermission(_session, null!, "Admin"));
        }

        [Fact]
        public void SetPermission_NullRoles_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _service.SetPermission(_session, "/", null!));
        }
    }
}
