using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class PveHttpClientTicketRenewalTests
    {
        private const string Username = "root@pam";

        // A real ticket ends in a base64 signature, so the fixture carries the three base64
        // characters the form encoder rewrites; the expected body below is the encoded form.
        private const string OldTicket = "PVE:root@pam:68B7ABCD::aB+c/d==";
        private const string OldTicketEncoded = "PVE:root@pam:68B7ABCD::aB%2Bc/d%3D%3D";
        private const string OldCsrf = "CSRF-OLD";
        private const string NewTicket = "PVE:root@pam:68B7BEEF::eF+g/h==";
        private const string NewCsrf = "CSRF-NEW";

        private const string RenewalBody = "username=" + Username + "&password=" + OldTicketEncoded;
        private const string TicketOk =
            "{\"data\":{\"ticket\":\"" + NewTicket + "\",\"CSRFPreventionToken\":\"" + NewCsrf + "\",\"username\":\"root@pam\"}}";
        private const string Unauthorized = "{\"data\":null,\"message\":\"authentication failure\"}";
        private const string DataOk = "{\"data\":{}}";

        private static readonly DateTime Now = new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

        private static Task NoDelay(TimeSpan _) => Task.CompletedTask;

        private static PveSession TicketSession(DateTime expiry) =>
            new PveSession("pve.example.com", 8006, false, Username, OldTicket, OldCsrf, expiry);

        private static PveSession TokenSession() =>
            new PveSession("pve.example.com", 8006, false, "root@pam!token=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        private static PveHttpClient NewClient(PveSession session, HttpMessageHandler handler,
            Func<DateTime>? clock = null, TimeSpan? timeoutOverride = null) =>
            new PveHttpClient(session, timeoutOverride, guestLockRetryWindow: null, handler, NoDelay,
                handlerCache: null, utcNow: clock ?? (() => Now));

        private static bool IsTicketPost(Recorded r) =>
            r.Method == HttpMethod.Post && r.Uri.EndsWith("/access/ticket", StringComparison.Ordinal);

        [Fact]
        public async Task PutAsync_RenewsOnceAfter401AndRetriesWithTheNewTicket()
        {
            var session = TicketSession(Now.AddHours(2));
            var handler = new RecordingHandler(
                (HttpStatusCode.Unauthorized, Unauthorized),
                (HttpStatusCode.OK, TicketOk),
                (HttpStatusCode.OK, DataOk));

            using (var client = NewClient(session, handler))
            {
                var result = await client.PutAsync("nodes/pve9a/qemu/100/config",
                    new Dictionary<string, string> { ["cores"] = "2" });
                Assert.Equal(DataOk, result);
            }

            Assert.Equal(3, handler.Requests.Count);

            var first = handler.Requests[0];
            Assert.Equal(HttpMethod.Put, first.Method);
            Assert.Equal("PVEAuthCookie=" + OldTicket, first.Cookie);
            Assert.Equal(OldCsrf, first.Csrf);

            var renewal = handler.Requests[1];
            Assert.True(IsTicketPost(renewal));
            Assert.Equal(RenewalBody, renewal.Body);
            Assert.Null(renewal.Cookie);
            Assert.Null(renewal.Csrf);

            var retry = handler.Requests[2];
            Assert.Equal(HttpMethod.Put, retry.Method);
            Assert.Equal(first.Uri, retry.Uri);
            Assert.Equal("cores=2", retry.Body);
            Assert.Equal("PVEAuthCookie=" + NewTicket, retry.Cookie);
            Assert.Equal(NewCsrf, retry.Csrf);

            Assert.Equal(NewTicket, session.Ticket);
            Assert.Equal(NewCsrf, session.CsrfToken);
            Assert.Equal(Now.AddHours(2), session.TicketExpiry);
        }

        [Fact]
        public async Task GetAsync_RenewsBeforeSendingWhenTheTicketIsPastHalfLife()
        {
            var session = TicketSession(Now.AddMinutes(59));
            var handler = new RecordingHandler(
                (HttpStatusCode.OK, TicketOk),
                (HttpStatusCode.OK, DataOk),
                (HttpStatusCode.OK, DataOk));

            using (var client = NewClient(session, handler))
            {
                await client.GetAsync("nodes/pve9a/status");
                await client.GetAsync("nodes/pve9a/status");
            }

            Assert.Equal(3, handler.Requests.Count);
            Assert.True(IsTicketPost(handler.Requests[0]));
            Assert.Equal(RenewalBody, handler.Requests[0].Body);
            Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
            Assert.Equal("PVEAuthCookie=" + NewTicket, handler.Requests[1].Cookie);
            Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
            Assert.Equal("PVEAuthCookie=" + NewTicket, handler.Requests[2].Cookie);
            Assert.Equal(1, handler.Requests.Count(IsTicketPost));
        }

        [Fact]
        public async Task GetAsync_DoesNotRenewBeforeHalfLife()
        {
            var session = TicketSession(Now.AddMinutes(61));
            var handler = new RecordingHandler((HttpStatusCode.OK, DataOk));

            using (var client = NewClient(session, handler))
            {
                await client.GetAsync("nodes/pve9a/status");
            }

            var only = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, only.Method);
            Assert.Equal("PVEAuthCookie=" + OldTicket, only.Cookie);
            Assert.Equal(OldTicket, session.Ticket);
        }

        [Fact]
        public async Task GetAsync_RenewsExactlyAtHalfLife()
        {
            var session = TicketSession(Now.AddHours(1));
            var handler = new RecordingHandler(
                (HttpStatusCode.OK, TicketOk),
                (HttpStatusCode.OK, DataOk));

            using (var client = NewClient(session, handler))
            {
                await client.GetAsync("nodes/pve9a/status");
            }

            Assert.Equal(2, handler.Requests.Count);
            Assert.True(IsTicketPost(handler.Requests[0]));
        }

        [Fact]
        public async Task GetAsync_ExpiredTicketThrowsSessionExpiredWithoutSendingAnything()
        {
            var session = TicketSession(Now.AddMinutes(-1));
            var handler = new RecordingHandler((HttpStatusCode.OK, TicketOk));

            using (var client = NewClient(session, handler))
            {
                await Assert.ThrowsAsync<PveSessionExpiredException>(() => client.GetAsync("nodes/pve9a/status"));
            }

            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task GetAsync_ThrowsSessionExpiredWhenTheRenewalIsRejectedToo()
        {
            var session = TicketSession(Now.AddHours(2));
            var handler = new RecordingHandler(
                (HttpStatusCode.Unauthorized, Unauthorized),
                (HttpStatusCode.Unauthorized, Unauthorized),
                (HttpStatusCode.OK, DataOk));

            using (var client = NewClient(session, handler))
            {
                var ex = await Assert.ThrowsAsync<PveSessionExpiredException>(
                    () => client.GetAsync("nodes/pve9a/status"));

                var inner = Assert.IsType<PveApiException>(ex.InnerException);
                Assert.Equal(HttpStatusCode.Unauthorized, inner.StatusCode);
                Assert.Equal("nodes/pve9a/status", inner.Resource);
                Assert.Equal("GET", inner.HttpMethod);
                Assert.Contains("Ticket renewal failed", ex.Message);
            }

            Assert.Equal(2, handler.Requests.Count);
            Assert.True(IsTicketPost(handler.Requests[1]));
            Assert.Equal(OldTicket, session.Ticket);
        }

        [Fact]
        public async Task GetAsync_ReactiveRenewalThatFailsForAnotherReasonStillReportsTheOriginal401()
        {
            var session = TicketSession(Now.AddHours(2));
            var handler = new RecordingHandler(
                (HttpStatusCode.Unauthorized, Unauthorized),
                (HttpStatusCode.ServiceUnavailable, "{\"data\":null}"));

            using (var client = NewClient(session, handler))
            {
                var ex = await Assert.ThrowsAsync<PveSessionExpiredException>(
                    () => client.GetAsync("nodes/pve9a/status"));

                var inner = Assert.IsType<PveApiException>(ex.InnerException);
                Assert.Equal(HttpStatusCode.Unauthorized, inner.StatusCode);
                Assert.Contains("503", ex.Message);
            }

            Assert.Equal(2, handler.Requests.Count);
        }

        [Fact]
        public async Task GetAsync_ReactiveRenewalWithAMalformedBodyReportsTheOriginal401()
        {
            var session = TicketSession(Now.AddHours(2));
            var handler = new RecordingHandler(
                (HttpStatusCode.Unauthorized, Unauthorized),
                (HttpStatusCode.OK, "{\"data\":null}"));

            using (var client = NewClient(session, handler))
            {
                var ex = await Assert.ThrowsAsync<PveSessionExpiredException>(
                    () => client.GetAsync("nodes/pve9a/status"));

                var inner = Assert.IsType<PveApiException>(ex.InnerException);
                Assert.Equal(HttpStatusCode.Unauthorized, inner.StatusCode);
            }

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(OldTicket, session.Ticket);
        }

        [Fact]
        public async Task GetAsync_RetriesOnlyOnceAfterRenewal()
        {
            var session = TicketSession(Now.AddHours(2));
            var handler = new RecordingHandler(
                (HttpStatusCode.Unauthorized, Unauthorized),
                (HttpStatusCode.OK, TicketOk),
                (HttpStatusCode.Unauthorized, Unauthorized),
                (HttpStatusCode.OK, TicketOk),
                (HttpStatusCode.OK, DataOk));

            using (var client = NewClient(session, handler))
            {
                var ex = await Assert.ThrowsAsync<PveSessionExpiredException>(
                    () => client.GetAsync("nodes/pve9a/status"));
                var inner = Assert.IsType<PveApiException>(ex.InnerException);
                Assert.Equal(HttpStatusCode.Unauthorized, inner.StatusCode);
                Assert.Equal("nodes/pve9a/status", inner.Resource);
            }

            Assert.Equal(3, handler.Requests.Count);
            Assert.Equal(1, handler.Requests.Count(IsTicketPost));
            Assert.Equal("PVEAuthCookie=" + NewTicket, handler.Requests[2].Cookie);
        }

        [Fact]
        public async Task GetAsync_ProactiveRenewalThatFailsForAnotherReasonKeepsUsingTheCurrentTicket()
        {
            var session = TicketSession(Now.AddMinutes(30));
            var handler = new RecordingHandler(
                (HttpStatusCode.ServiceUnavailable, "{\"data\":null}"),
                (HttpStatusCode.OK, DataOk));

            using (var client = NewClient(session, handler))
            {
                var result = await client.GetAsync("nodes/pve9a/status");
                Assert.Equal(DataOk, result);
            }

            Assert.Equal(2, handler.Requests.Count);
            Assert.True(IsTicketPost(handler.Requests[0]));
            Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
            Assert.Equal("PVEAuthCookie=" + OldTicket, handler.Requests[1].Cookie);
            Assert.Equal(OldTicket, session.Ticket);
        }

        [Fact]
        public async Task GetAsync_ProactiveRenewalRejectedWith401ThrowsSessionExpired()
        {
            var session = TicketSession(Now.AddMinutes(30));
            var handler = new RecordingHandler(
                (HttpStatusCode.Unauthorized, Unauthorized),
                (HttpStatusCode.OK, DataOk));

            using (var client = NewClient(session, handler))
            {
                var ex = await Assert.ThrowsAsync<PveSessionExpiredException>(
                    () => client.GetAsync("nodes/pve9a/status"));
                var inner = Assert.IsType<PveApiException>(ex.InnerException);
                Assert.Equal(HttpStatusCode.Unauthorized, inner.StatusCode);
                Assert.Equal("access/ticket", inner.Resource);
            }

            var only = Assert.Single(handler.Requests);
            Assert.True(IsTicketPost(only));
        }

        [Fact]
        public async Task GetAsync_RenewalIsBoundedByTheSessionTimeoutNotTheClientOverride()
        {
            var session = TicketSession(Now.AddHours(2));
            session.Timeout = TimeSpan.FromMilliseconds(200);
            var handler = new RecordingHandler(async (request, cancellationToken) =>
            {
                if (IsTicketPost(request))
                {
                    await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, cancellationToken);
                }
                return (HttpStatusCode.Unauthorized, Unauthorized);
            });

            using (var client = NewClient(session, handler, timeoutOverride: System.Threading.Timeout.InfiniteTimeSpan))
            {
                var ex = await Assert.ThrowsAsync<PveSessionExpiredException>(
                    () => client.GetAsync("nodes/pve9a/status"));
                Assert.Contains("timed out after 0.2s", ex.Message);
            }

            Assert.Equal(2, handler.Requests.Count);
        }

        [Fact]
        public async Task GetAsync_ApiTokenSessionSurfacesThe401AndNeverPostsATicket()
        {
            var session = TokenSession();
            var handler = new RecordingHandler(
                (HttpStatusCode.Unauthorized, Unauthorized),
                (HttpStatusCode.OK, TicketOk),
                (HttpStatusCode.OK, DataOk));

            using (var client = NewClient(session, handler))
            {
                var ex = await Assert.ThrowsAsync<PveApiException>(
                    () => client.GetAsync("nodes/pve9a/status"));
                Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
            }

            var only = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, only.Method);
            Assert.StartsWith("PVEAPIToken=", only.Authorization);
            Assert.Null(only.Cookie);
        }

        [Fact]
        public async Task GetAsync_ACallerArrivingDuringARenewalJoinsItInsteadOfPostingAgain()
        {
            var session = TicketSession(Now.AddMinutes(10));
            var ticketPostArrived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseTicketPost = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new RecordingHandler(async (request, _) =>
            {
                if (IsTicketPost(request))
                {
                    ticketPostArrived.TrySetResult(true);
                    await releaseTicketPost.Task;
                    return (HttpStatusCode.OK, TicketOk);
                }
                return (HttpStatusCode.OK, DataOk);
            });

            // The second client's first clock read happens after it has taken its ticket
            // snapshot, so awaiting it proves the snapshot is the stale one.
            var secondRead = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var shared = new PassThroughHandler(handler);
            using (var first = NewClient(session, shared))
            using (var second = NewClient(session, shared, clock: () => { secondRead.TrySetResult(true); return Now; }))
            {
                var a = first.GetAsync("nodes/pve9a/status");
                await ticketPostArrived.Task;

                var b = second.GetAsync("nodes/pve9a/qemu/100/status/current");
                await secondRead.Task;

                releaseTicketPost.TrySetResult(true);
                await Task.WhenAll(a, b);
            }

            Assert.Equal(1, handler.Requests.Count(IsTicketPost));
            var gets = handler.Requests.Where(r => r.Method == HttpMethod.Get).ToList();
            Assert.Equal(2, gets.Count);
            Assert.All(gets, g => Assert.Equal("PVEAuthCookie=" + NewTicket, g.Cookie));
        }

        [Fact]
        public async Task GetAsync_ACallerHoldingAnAlreadyReplacedTicketTakesTheReplacementWithoutPosting()
        {
            var session = TicketSession(Now.AddMinutes(10));
            var handler = new RecordingHandler(request =>
                IsTicketPost(request) ? (HttpStatusCode.OK, TicketOk) : (HttpStatusCode.OK, DataOk));

            var shared = new PassThroughHandler(handler);
            using (var first = NewClient(session, shared))
            {
                // The second client's clock runs between its ticket snapshot and its renewal
                // attempt; completing the first client's request there replaces the ticket
                // underneath the second client.
                var renewedByFirst = false;
                using (var second = NewClient(session, shared, clock: () =>
                {
                    if (!renewedByFirst)
                    {
                        renewedByFirst = true;
                        first.Get("nodes/pve9a/status");
                    }
                    return Now;
                }))
                {
                    await second.GetAsync("nodes/pve9a/qemu/100/status/current");
                }
            }

            Assert.Equal(1, handler.Requests.Count(IsTicketPost));
            var gets = handler.Requests.Where(r => r.Method == HttpMethod.Get).ToList();
            Assert.Equal(2, gets.Count);
            Assert.All(gets, g => Assert.Equal("PVEAuthCookie=" + NewTicket, g.Cookie));
        }

        [Fact]
        public async Task GetAsync_ManyParallelRequestsOnOnePastHalfLifeSessionProduceOneTicketPost()
        {
            const int parallelism = 16;
            var session = TicketSession(Now.AddMinutes(10));
            var handler = new RecordingHandler(request =>
            {
                if (IsTicketPost(request))
                {
                    Thread.Sleep(50);
                    return (HttpStatusCode.OK, TicketOk);
                }
                return (HttpStatusCode.OK, DataOk);
            });

            using (var client = NewClient(session, handler))
            using (var start = new ManualResetEventSlim(false))
            {
                var workers = Enumerable.Range(0, parallelism)
                    .Select(i => Task.Factory.StartNew(() =>
                    {
                        start.Wait();
                        return client.GetAsync($"nodes/pve9a/qemu/{100 + i}/status/current");
                    }, TaskCreationOptions.LongRunning).Unwrap())
                    .ToArray();
                start.Set();
                await Task.WhenAll(workers);
            }

            Assert.Equal(1, handler.Requests.Count(IsTicketPost));
            var gets = handler.Requests.Where(r => r.Method == HttpMethod.Get).ToList();
            Assert.Equal(parallelism, gets.Count);
            Assert.All(gets, g => Assert.Equal("PVEAuthCookie=" + NewTicket, g.Cookie));
            Assert.Equal(NewTicket, session.Ticket);
        }

        [Fact]
        public async Task UploadFileAsync_RenewsBeforeSendingWhenTheTicketIsPastHalfLife()
        {
            var session = TicketSession(Now.AddMinutes(30));
            var handler = new RecordingHandler(
                (HttpStatusCode.OK, TicketOk),
                (HttpStatusCode.OK, DataOk));

            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".iso");
            File.WriteAllText(path, "not really an iso");
            try
            {
                using (var client = NewClient(session, handler))
                {
                    await client.UploadFileAsync("nodes/pve9a/storage/local/upload", path,
                        new Dictionary<string, string> { ["content"] = "iso" });
                }
            }
            finally
            {
                File.Delete(path);
            }

            Assert.Equal(2, handler.Requests.Count);
            Assert.True(IsTicketPost(handler.Requests[0]));
            var upload = handler.Requests[1];
            Assert.Equal(HttpMethod.Post, upload.Method);
            Assert.Equal("PVEAuthCookie=" + NewTicket, upload.Cookie);
            Assert.Equal(NewCsrf, upload.Csrf);
        }

        private sealed class Recorded
        {
            public HttpMethod Method { get; set; } = HttpMethod.Get;
            public string Uri { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
            public string? Cookie { get; set; }
            public string? Csrf { get; set; }
            public string? Authorization { get; set; }
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly Func<Recorded, CancellationToken, Task<(HttpStatusCode status, string body)>> _respond;
            private readonly object _sync = new object();
            private readonly List<Recorded> _requests = new List<Recorded>();

            public IReadOnlyList<Recorded> Requests
            {
                get { lock (_sync) return _requests.ToList(); }
            }

            public RecordingHandler(params (HttpStatusCode status, string body)[] responses)
            {
                var index = 0;
                _respond = (_, __) =>
                {
                    var i = Interlocked.Increment(ref index) - 1;
                    if (i >= responses.Length)
                        throw new InvalidOperationException("RecordingHandler ran out of responses.");
                    return Task.FromResult(responses[i]);
                };
            }

            public RecordingHandler(Func<Recorded, (HttpStatusCode status, string body)> respond)
            {
                _respond = (r, _) => Task.FromResult(respond(r));
            }

            public RecordingHandler(Func<Recorded, CancellationToken, Task<(HttpStatusCode status, string body)>> respond)
            {
                _respond = respond;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var recorded = new Recorded
                {
                    Method = request.Method,
                    Uri = request.RequestUri!.ToString(),
                    Body = request.Content == null
                        ? string.Empty
                        : await request.Content.ReadAsStringAsync().ConfigureAwait(false),
                    Cookie = Header(request, "Cookie"),
                    Csrf = Header(request, "CSRFPreventionToken"),
                    Authorization = Header(request, "Authorization"),
                };
                lock (_sync) _requests.Add(recorded);

                var (status, body) = await _respond(recorded, cancellationToken).ConfigureAwait(false);
                return new HttpResponseMessage(status) { Content = new StringContent(body) };
            }

            private static string? Header(HttpRequestMessage request, string name) =>
                request.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
        }

        /// <summary>
        /// Lets several clients, each of which disposes its own handler, share one recorder.
        /// </summary>
        private sealed class PassThroughHandler : HttpMessageHandler
        {
            private readonly HttpMessageInvoker _inner;

            public PassThroughHandler(HttpMessageHandler inner)
            {
                _inner = new HttpMessageInvoker(inner, disposeHandler: false);
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                _inner.SendAsync(request, cancellationToken);
        }
    }
}
