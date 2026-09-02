using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Client
{
    public class PveHandlerCacheTests
    {
        private static PveSession NewSession(bool skipCertificateCheck = true, string host = "pve.example.com") =>
            new PveSession(host, 8006, skipCertificateCheck,
                "root@pam!token=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        private sealed class CannedHandler : HttpClientHandler
        {
            public int Sends;
            public bool IsDisposed;

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (IsDisposed) throw new ObjectDisposedException(nameof(CannedHandler));
                Interlocked.Increment(ref Sends);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":{\"version\":\"8.2\"}}")
                });
            }
        }

        private static (PveHandlerCache cache, List<CannedHandler> built) NewCache()
        {
            var built = new List<CannedHandler>();
            var cache = new PveHandlerCache(_ =>
            {
                var h = new CannedHandler();
                built.Add(h);
                return h;
            });
            return (cache, built);
        }

        [Fact]
        public void TwoClientsForTheSameEndpointShareOneHandler()
        {
            var (cache, built) = NewCache();

            using var first = new PveHttpClient(NewSession(), cache);
            using var second = new PveHttpClient(NewSession(), cache);

            Assert.Single(built);
            Assert.Same(first.Handler, second.Handler);
            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public void DifferentSkipCertificateCheckGetsADifferentHandler()
        {
            var (cache, built) = NewCache();

            using var insecure = new PveHttpClient(NewSession(skipCertificateCheck: true), cache);
            using var verified = new PveHttpClient(NewSession(skipCertificateCheck: false), cache);

            Assert.Equal(2, built.Count);
            Assert.NotSame(insecure.Handler, verified.Handler);
        }

        [Fact]
        public void DifferentHostGetsADifferentHandler()
        {
            var (cache, _) = NewCache();

            using var a = new PveHttpClient(NewSession(host: "pve-a.example.com"), cache);
            using var b = new PveHttpClient(NewSession(host: "pve-b.example.com"), cache);

            Assert.NotSame(a.Handler, b.Handler);
        }

        [Fact]
        public async Task DisposingOneClientLeavesTheSharedHandlerUsableByAnother()
        {
            var (cache, built) = NewCache();

            var first = new PveHttpClient(NewSession(), cache);
            using var second = new PveHttpClient(NewSession(), cache);
            first.Dispose();

            var body = await second.GetAsync("version");

            Assert.Contains("8.2", body);
            Assert.Equal(1, built[0].Sends);
            Assert.Single(built);
            Assert.False(built[0].IsDisposed);
        }

        [Fact]
        public void AnExplicitHandlerBypassesTheCacheAndIsOwnedByTheClient()
        {
            var (cache, built) = NewCache();
            var own = new CannedHandler();

            var client = new PveHttpClient(NewSession(), timeoutOverride: null,
                guestLockRetryWindow: null, handler: own, guestLockRetryDelay: null, handlerCache: cache);

            Assert.Same(own, client.Handler);
            Assert.Empty(built);

            client.Dispose();

            Assert.True(own.IsDisposed);
        }

        [Fact]
        public void SharedHandlersDoNotCarryACookieContainer()
        {
            var handler = PveHandlerCache.Shared.Get("cookies.example.invalid", 8006, skipCertificateCheck: false);

            Assert.False(handler.UseCookies);
        }

        [Fact]
        public void GetBuildsEachKeyOnceUnderConcurrentCallers()
        {
            var (cache, built) = NewCache();
            var handlers = new HttpClientHandler[32];

            Parallel.For(0, handlers.Length, i =>
                handlers[i] = cache.Get("pve.example.com", 8006, skipCertificateCheck: true));

            Assert.Single(built);
            Assert.All(handlers, h => Assert.Same(built[0], h));
        }
    }
}
