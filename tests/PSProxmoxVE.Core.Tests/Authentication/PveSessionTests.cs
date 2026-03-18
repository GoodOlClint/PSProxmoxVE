using System;
using Xunit;
using PSProxmoxVE.Core.Authentication;

namespace PSProxmoxVE.Core.Tests.Authentication
{
    public class PveSessionTests
    {
        private const string TestHostname = "pve.example.com";
        private const int TestPort = 8006;

        [Fact]
        public void TicketSession_NotExpired_OnCreation()
        {
            var expiry = DateTime.UtcNow.AddHours(2);
            var session = new PveSession(TestHostname, TestPort, false, "PVE:root@pam:TICKET", "CSRFTOKEN", expiry);

            Assert.False(session.IsExpired);
        }

        [Fact]
        public void TicketSession_IsExpired_After2Hours()
        {
            // Simulate a ticket that already expired an hour ago
            var expiry = DateTime.UtcNow.AddHours(-1);
            var session = new PveSession(TestHostname, TestPort, false, "PVE:root@pam:OLDTICKET", "CSRFTOKEN", expiry);

            Assert.True(session.IsExpired);
        }

        [Fact]
        public void ApiTokenSession_NeverExpires()
        {
            var session = new PveSession(TestHostname, TestPort, false, "root@pam!mytoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            Assert.False(session.IsExpired);
            Assert.Equal(PveAuthMode.ApiToken, session.AuthMode);
        }

        [Fact]
        public void BaseUrl_CorrectFormat()
        {
            var session = new PveSession(TestHostname, TestPort, false, "root@pam!mytoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            Assert.Equal($"https://{TestHostname}:{TestPort}/api2/json/", session.BaseUrl);
        }

        [Fact]
        public void TicketSession_AuthMode_IsTicket()
        {
            var expiry = DateTime.UtcNow.AddHours(2);
            var session = new PveSession(TestHostname, TestPort, false, "PVE:root@pam:TICKET", "CSRFTOKEN", expiry);

            Assert.Equal(PveAuthMode.Ticket, session.AuthMode);
        }

        [Fact]
        public void SeparateSessions_DoNotShareState()
        {
            var expiry1 = DateTime.UtcNow.AddHours(2);
            var session1 = new PveSession("host1", 8006, false, "PVE:root@pam:TICKET1", "CSRF1", expiry1);
            var session2 = new PveSession("host2", 8006, false, "root@pam!token=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            Assert.NotEqual(session1.Hostname, session2.Hostname);
            Assert.Equal(PveAuthMode.Ticket, session1.AuthMode);
            Assert.Equal(PveAuthMode.ApiToken, session2.AuthMode);
            Assert.Equal("PVE:root@pam:TICKET1", session1.Ticket);
            Assert.Null(session1.ApiToken);
            Assert.Null(session2.Ticket);
            Assert.NotNull(session2.ApiToken);
        }

        [Fact]
        public void TicketSession_StoresTicketAndCsrf()
        {
            var expiry = DateTime.UtcNow.AddHours(2);
            const string ticket = "PVE:root@pam:ABCD1234";
            const string csrf = "CSRFPREVENTION";

            var session = new PveSession(TestHostname, TestPort, false, ticket, csrf, expiry);

            Assert.Equal(ticket, session.Ticket);
            Assert.Equal(csrf, session.CsrfToken);
        }

        [Fact]
        public void ApiTokenSession_StoresApiToken()
        {
            const string token = "root@pam!mytoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            var session = new PveSession(TestHostname, TestPort, false, token);

            Assert.Equal(token, session.ApiToken);
        }

        [Fact]
        public void SkipCertificateCheck_IsStored()
        {
            var session = new PveSession(TestHostname, TestPort, skipCertificateCheck: true,
                "root@pam!mytoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            Assert.True(session.SkipCertificateCheck);
        }
    }
}
