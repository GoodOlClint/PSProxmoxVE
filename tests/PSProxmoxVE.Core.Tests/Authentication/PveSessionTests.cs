using System;
using System.Linq;
using System.Reflection;
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
            var session = new PveSession(TestHostname, TestPort, false, "root@pam", "PVE:root@pam:TICKET", "CSRFTOKEN", expiry);

            Assert.False(session.IsExpired);
        }

        [Fact]
        public void TicketSession_IsExpired_After2Hours()
        {
            // Simulate a ticket that already expired an hour ago
            var expiry = DateTime.UtcNow.AddHours(-1);
            var session = new PveSession(TestHostname, TestPort, false, "root@pam", "PVE:root@pam:OLDTICKET", "CSRFTOKEN", expiry);

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
            var session = new PveSession(TestHostname, TestPort, false, "root@pam", "PVE:root@pam:TICKET", "CSRFTOKEN", expiry);

            Assert.Equal(PveAuthMode.Ticket, session.AuthMode);
        }

        [Fact]
        public void SeparateSessions_DoNotShareState()
        {
            var expiry1 = DateTime.UtcNow.AddHours(2);
            var session1 = new PveSession("host1", 8006, false, "root@pam", "PVE:root@pam:TICKET1", "CSRF1", expiry1);
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

            var session = new PveSession(TestHostname, TestPort, false, "root@pam", ticket, csrf, expiry);

            Assert.Equal(ticket, session.Ticket);
            Assert.Equal(csrf, session.CsrfToken);
        }

        [Fact]
        public void TicketSession_StoresUsername()
        {
            var session = new PveSession(TestHostname, TestPort, false, "admin@pve", "PVE:admin@pve:TICKET", "CSRF", DateTime.UtcNow.AddHours(2));

            Assert.Equal("admin@pve", session.Username);
        }

        [Fact]
        public void ApiTokenSession_HasNoUsername()
        {
            var session = new PveSession(TestHostname, TestPort, false, "root@pam!mytoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            Assert.Null(session.Username);
        }

        [Fact]
        public void TicketSession_RejectsNullUsername()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PveSession(TestHostname, TestPort, false, null!, "PVE:root@pam:TICKET", "CSRF", DateTime.UtcNow.AddHours(2)));
        }

        [Fact]
        public void Renewal_SecondCallerJoinsTheClaimedRenewalAndSharesItsResult()
        {
            var session = new PveSession(TestHostname, TestPort, false, "root@pam", "PVE:root@pam:OLD", "CSRF-OLD", DateTime.UtcNow.AddHours(2));
            var stale = session.ReadTicket()!;

            var first = session.JoinOrClaimRenewal(stale, out var claimed);
            var second = session.JoinOrClaimRenewal(stale, out var secondClaim);

            Assert.NotNull(claimed);
            Assert.Null(secondClaim);
            Assert.Same(first, second);
            Assert.False(first.IsCompleted);

            var renewed = new PveSession.TicketState("PVE:root@pam:NEW", "CSRF-NEW", DateTime.UtcNow.AddHours(2));
            session.CompleteRenewal(claimed!, renewed);

            Assert.Same(renewed, first.Result);
            Assert.Same(renewed, session.ReadTicket());
            Assert.Equal("PVE:root@pam:NEW", session.Ticket);
        }

        [Fact]
        public void Renewal_SecondCallerJoinsTheClaimedRenewalAndSharesItsFailure()
        {
            var session = new PveSession(TestHostname, TestPort, false, "root@pam", "PVE:root@pam:OLD", "CSRF-OLD", DateTime.UtcNow.AddHours(2));
            var stale = session.ReadTicket()!;

            var first = session.JoinOrClaimRenewal(stale, out var claimed);
            var second = session.JoinOrClaimRenewal(stale, out _);
            var failure = new InvalidOperationException("boom");
            session.FailRenewal(claimed!, failure);

            Assert.True(first.IsFaulted);
            Assert.True(second.IsFaulted);
            Assert.Same(failure, first.Exception!.InnerException);
            Assert.Same(failure, second.Exception!.InnerException);
            Assert.Same(stale, session.ReadTicket());

            var retry = session.JoinOrClaimRenewal(stale, out var retryClaim);
            Assert.NotNull(retryClaim);
            Assert.False(retry.IsCompleted);
        }

        [Fact]
        public void Renewal_CallerHoldingAReplacedTicketGetsTheReplacementWithoutClaiming()
        {
            var session = new PveSession(TestHostname, TestPort, false, "root@pam", "PVE:root@pam:OLD", "CSRF-OLD", DateTime.UtcNow.AddHours(2));
            var stale = session.ReadTicket()!;
            var renewed = new PveSession.TicketState("PVE:root@pam:NEW", "CSRF-NEW", DateTime.UtcNow.AddHours(2));
            session.JoinOrClaimRenewal(stale, out var claimed);
            session.CompleteRenewal(claimed!, renewed);

            var late = session.JoinOrClaimRenewal(stale, out var lateClaim);

            Assert.Null(lateClaim);
            Assert.True(late.IsCompleted);
            Assert.Same(renewed, late.Result);
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

        [Theory]
        [InlineData("ApiToken")]
        [InlineData("Ticket")]
        [InlineData("CsrfToken")]
        public void CredentialProperties_AreNotPubliclyReadable(string propertyName)
        {
            var property = typeof(PveSession).GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(property);
            Assert.False(property!.GetMethod!.IsPublic);
        }

        [Fact]
        public void PublicProperties_ExposeNoApiToken()
        {
            const string token = "root@pam!mytoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            var session = new PveSession(TestHostname, TestPort, false, token);

            Assert.DoesNotContain(RenderPublicProperties(session), v => v.Contains(token));
        }

        [Fact]
        public void PublicProperties_ExposeNoTicketOrCsrfToken()
        {
            const string ticket = "PVE:root@pam:SECRETTICKET";
            const string csrf = "SECRETCSRFTOKEN";
            var session = new PveSession(TestHostname, TestPort, false, "root@pam", ticket, csrf,
                DateTime.UtcNow.AddHours(2));

            var rendered = RenderPublicProperties(session);
            Assert.DoesNotContain(rendered, v => v.Contains(ticket));
            Assert.DoesNotContain(rendered, v => v.Contains(csrf));
        }

        private static string[] RenderPublicProperties(PveSession session) =>
            typeof(PveSession)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.GetValue(session)?.ToString())
                .Where(v => v != null)
                .ToArray()!;

        [Fact]
        public void Timeout_DefaultIs100Seconds()
        {
            var session = new PveSession(TestHostname, TestPort, false,
                "root@pam!mytoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            Assert.Equal(TimeSpan.FromSeconds(100), session.Timeout);
        }
    }
}
