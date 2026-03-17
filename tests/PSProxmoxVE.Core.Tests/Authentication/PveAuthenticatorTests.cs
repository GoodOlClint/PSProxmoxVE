using System;
using Xunit;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE.Core.Tests.Authentication
{
    /// <summary>
    /// Tests for PveAuthenticator input validation. These tests exercise the
    /// argument-validation logic without making actual HTTP calls.
    /// </summary>
    public class PveAuthenticatorTests
    {
        private const string ValidHostname = "pve.example.com";
        private const int ValidPort = 8006;

        [Fact]
        public void ValidateUsername_MissingAtSign_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                PveAuthenticator.AuthenticateWithCredentials(ValidHostname, ValidPort, false, "rootpam", "password"));

            Assert.Contains("realm", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateApiToken_InvalidFormat_NoTokenId_Throws()
        {
            // Missing the !TOKENID part — no exclamation mark
            var ex = Assert.Throws<ArgumentException>(() =>
                PveAuthenticator.AuthenticateWithApiToken(ValidHostname, ValidPort, false, "root@pam=someuuid"));

            Assert.Contains("token", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateApiToken_InvalidFormat_NoEquals_Throws()
        {
            // Has ! but no = sign after TOKENID
            var ex = Assert.Throws<ArgumentException>(() =>
                PveAuthenticator.AuthenticateWithApiToken(ValidHostname, ValidPort, false, "root@pam!mytoken"));

            Assert.Contains("token", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateApiToken_InvalidFormat_NoRealm_Throws()
        {
            // Missing the @realm portion
            var ex = Assert.Throws<ArgumentException>(() =>
                PveAuthenticator.AuthenticateWithApiToken(ValidHostname, ValidPort, false, "root!mytoken=someuuid"));

            Assert.Contains("token", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateHostname_NullOrEmpty_Throws_ForCredentials(string hostname)
        {
            Assert.Throws<ArgumentException>(() =>
                PveAuthenticator.AuthenticateWithCredentials(hostname, ValidPort, false, "root@pam", "password"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateHostname_NullOrEmpty_Throws_ForApiToken(string hostname)
        {
            Assert.Throws<ArgumentException>(() =>
                PveAuthenticator.AuthenticateWithApiToken(hostname, ValidPort, false, "root@pam!mytoken=uuid"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateApiToken_NullOrEmpty_Throws(string token)
        {
            Assert.Throws<ArgumentException>(() =>
                PveAuthenticator.AuthenticateWithApiToken(ValidHostname, ValidPort, false, token));
        }
    }
}
