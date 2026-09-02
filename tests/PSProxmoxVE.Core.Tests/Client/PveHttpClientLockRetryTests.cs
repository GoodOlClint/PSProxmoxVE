using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Exceptions;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Client
{
    public class PveHttpClientLockRetryTests
    {
        private const string LockTimeoutBody =
            "{\"message\":\"can't lock file '/var/lock/qemu-server/lock-100.conf' - got timeout\"}";

        // A no-op delay removes the retry loop's inter-attempt wait entirely, so these tests
        // carry no wall-clock dependence: the production 45s window is exhausted only if the
        // scripted responses themselves never resolve the lock, never by runner speed.
        private static Task NoDelay(TimeSpan _) => Task.CompletedTask;

        private static (PveHttpClient client, ScriptedHandler handler) NewClient(
            params (HttpStatusCode status, string body)[] responses) =>
            NewClient(window: null, responses);

        private static (PveHttpClient client, ScriptedHandler handler) NewClient(
            TimeSpan? window, params (HttpStatusCode status, string body)[] responses)
        {
            var session = new PveSession("pve.example.com", 8006, false,
                "root@pam!token=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var handler = new ScriptedHandler(responses);
            var client = new PveHttpClient(session, timeoutOverride: null, window, handler, NoDelay);
            return (client, handler);
        }

        private static Dictionary<string, string> ConfigBody() =>
            new Dictionary<string, string> { ["scsi0"] = "local-lvm:1" };

        [Fact]
        public async Task PutAsync_ReissuesTheRequestWhilePveReportsTheGuestFlock()
        {
            var (client, handler) = NewClient(
                (HttpStatusCode.InternalServerError, LockTimeoutBody),
                (HttpStatusCode.InternalServerError, LockTimeoutBody),
                (HttpStatusCode.OK, "{\"data\":null}"));

            using (client)
            {
                var result = await client.PutAsync("nodes/pve9a/qemu/100/config", ConfigBody());
                Assert.Equal("{\"data\":null}", result);
            }

            Assert.Equal(3, handler.Bodies.Count);
        }

        [Fact]
        public async Task PutAsync_DoesNotReissueWhenTheRetryWindowIsAlreadySpent()
        {
            var (client, handler) = NewClient(TimeSpan.Zero,
                (HttpStatusCode.InternalServerError, LockTimeoutBody),
                (HttpStatusCode.OK, "{\"data\":null}"));

            using (client)
            {
                await Assert.ThrowsAsync<PveApiException>(
                    () => client.PutAsync("nodes/pve9a/qemu/100/config", ConfigBody()));
            }

            Assert.Single(handler.Bodies);
        }

        [Fact]
        public async Task PutAsync_RebuildsTheRequestSoEveryAttemptCarriesTheSameBody()
        {
            var (client, handler) = NewClient(
                (HttpStatusCode.InternalServerError, LockTimeoutBody),
                (HttpStatusCode.OK, "{\"data\":null}"));

            using (client)
            {
                await client.PutAsync("nodes/pve9a/qemu/100/config", ConfigBody());
            }

            Assert.Equal(2, handler.Bodies.Count);
            Assert.Equal("scsi0=local-lvm:1", handler.Bodies[0]);
            Assert.Equal(handler.Bodies[0], handler.Bodies[1]);
            Assert.All(handler.Methods, m => Assert.Equal(HttpMethod.Put, m));
            Assert.Equal(handler.Uris[0], handler.Uris[1]);
            Assert.EndsWith("nodes/pve9a/qemu/100/config", handler.Uris[0]);
        }

        [Fact]
        public async Task PostAsync_DoesNotReissueApiErrorsThatAreNotTheFlock()
        {
            var (client, handler) = NewClient(
                (HttpStatusCode.InternalServerError, "{\"message\":\"VM 100 not running\"}"),
                (HttpStatusCode.OK, "{\"data\":null}"));

            using (client)
            {
                var ex = await Assert.ThrowsAsync<PveApiException>(
                    () => client.PostAsync("nodes/pve9a/qemu/100/status/reset"));
                Assert.Contains("VM 100 not running", ex.Message);
            }

            Assert.Single(handler.Bodies);
        }

        [Fact]
        public async Task GetAsync_ReissuesWithoutCarryingContent()
        {
            var (client, handler) = NewClient(
                (HttpStatusCode.InternalServerError, LockTimeoutBody),
                (HttpStatusCode.OK, "{\"data\":{}}"));

            using (client)
            {
                await client.GetAsync("nodes/pve9a/qemu/100/status/current");
            }

            Assert.Equal(2, handler.Bodies.Count);
            Assert.All(handler.Bodies, b => Assert.Equal(string.Empty, b));
        }

        private sealed class ScriptedHandler : HttpMessageHandler
        {
            private readonly (HttpStatusCode status, string body)[] _responses;
            private int _index;

            public List<string> Bodies { get; } = new List<string>();
            public List<HttpMethod> Methods { get; } = new List<HttpMethod>();
            public List<string> Uris { get; } = new List<string>();

            public ScriptedHandler((HttpStatusCode status, string body)[] responses)
            {
                _responses = responses;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Bodies.Add(request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync().ConfigureAwait(false));
                Methods.Add(request.Method);
                Uris.Add(request.RequestUri!.ToString());

                if (_index >= _responses.Length)
                    throw new InvalidOperationException("ScriptedHandler ran out of responses.");

                var (status, body) = _responses[_index++];
                return new HttpResponseMessage(status) { Content = new StringContent(body) };
            }
        }
    }
}
