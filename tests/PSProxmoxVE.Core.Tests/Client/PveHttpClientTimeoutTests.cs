using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
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

        private static void SetInnerHttpClient(PveHttpClient client, HttpClient newInner)
        {
            var field = typeof(PveHttpClient).GetField("_httpClient",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            ((HttpClient)field.GetValue(client)!).Dispose();
            field.SetValue(client, newInner);
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
        public async Task SendAsync_TimeoutFires_ThrowsPveApiExceptionWithRequestTimeout()
        {
            var session = NewSession();
            using var client = new PveHttpClient(session);

            // Swap in an HttpClient with a delaying handler and a 50ms timeout so
            // HttpClient.Timeout fires reliably without any real network.
            var delayingClient = new HttpClient(new DelayingHandler(TimeSpan.FromSeconds(30)))
            {
                Timeout = TimeSpan.FromMilliseconds(50)
            };
            SetInnerHttpClient(client, delayingClient);

            var ex = await Assert.ThrowsAsync<PveApiException>(() => client.GetAsync("version"));
            Assert.Equal(HttpStatusCode.RequestTimeout, ex.StatusCode);
            Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class DelayingHandler : HttpMessageHandler
        {
            private readonly TimeSpan _delay;
            public DelayingHandler(TimeSpan delay) { _delay = delay; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}
