using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Client
{
    public class PveHttpClientTimeoutTests
    {
        private static HttpClient GetInnerHttpClient(PveHttpClient client)
        {
            var field = typeof(PveHttpClient).GetField("_httpClient",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (HttpClient)field.GetValue(client)!;
        }

        private static PveSession NewSession()
        {
            return new PveSession("pve.example.com", 8006, false,
                "root@pam!token=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        }

        [Fact]
        public void Constructor_UsesSessionTimeoutByDefault()
        {
            var session = NewSession();
            session.Timeout = TimeSpan.FromSeconds(42);

            using var client = new PveHttpClient(session);

            Assert.Equal(TimeSpan.FromSeconds(42), GetInnerHttpClient(client).Timeout);
        }

        [Fact]
        public void Constructor_OverrideTakesPrecedenceOverSessionTimeout()
        {
            var session = NewSession();
            session.Timeout = TimeSpan.FromSeconds(42);

            using var client = new PveHttpClient(session, TimeSpan.FromMinutes(30));

            Assert.Equal(TimeSpan.FromMinutes(30), GetInnerHttpClient(client).Timeout);
        }

        [Fact]
        public void Constructor_InfiniteTimeSpanIsAccepted()
        {
            var session = NewSession();

            using var client = new PveHttpClient(session, Timeout.InfiniteTimeSpan);

            Assert.Equal(Timeout.InfiniteTimeSpan, GetInnerHttpClient(client).Timeout);
        }

        [Fact]
        public void DefaultSessionTimeoutIs100Seconds()
        {
            var session = NewSession();

            using var client = new PveHttpClient(session);

            Assert.Equal(TimeSpan.FromSeconds(100), GetInnerHttpClient(client).Timeout);
        }
    }
}
