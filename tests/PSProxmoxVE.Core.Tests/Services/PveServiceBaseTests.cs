using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using PSProxmoxVE.Core.Services;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Services
{
    public class PveServiceBaseTests
    {
        private static PveSession CreateSession() =>
            new PveSession("pve1.example.com", 8006, true, "PVE:root@pam:TEST_TOKEN");

        private sealed class RecordingClient : IPveHttpClient
        {
            public int DisposeCalls;
            public int Gets;

            public Task<string> GetAsync(string resource) { Gets++; return Task.FromResult("{\"data\":null}"); }
            public Task<string> PostAsync(string resource, Dictionary<string, string>? data = null) => throw new NotSupportedException();
            public Task<string> PostAsync(string resource, IEnumerable<KeyValuePair<string, string>> data) => throw new NotSupportedException();
            public Task<string> PutAsync(string resource, Dictionary<string, string>? data = null) => throw new NotSupportedException();
            public Task<string> DeleteAsync(string resource) => throw new NotSupportedException();
            public string Get(string resource) => GetAsync(resource).GetAwaiter().GetResult();
            public string Post(string resource, Dictionary<string, string>? data = null) => throw new NotSupportedException();
            public string Put(string resource, Dictionary<string, string>? data = null) => throw new NotSupportedException();
            public string Delete(string resource) => throw new NotSupportedException();
            public Task<string> UploadFileAsync(string resource, string filePath, Dictionary<string, string>? formFields = null,
                string? checksum = null, string? checksumAlgorithm = null, Action<long, long>? progressCallback = null) =>
                throw new NotSupportedException();
            public void Dispose() => DisposeCalls++;
        }

        private sealed class ProbeService : PveServiceBase
        {
            private readonly RecordingClient? _built;
            public TimeSpan? SeenTimeoutOverride;

            private ProbeService(RecordingClient built) { _built = built; }
            private ProbeService(IPveHttpClient injected) : base(injected) { }

            public static ProbeService Building(RecordingClient built) => new ProbeService(built);
            public static ProbeService Using(IPveHttpClient injected) => new ProbeService(injected);

            internal override IPveHttpClient CreateClient(PveSession session, TimeSpan? timeoutOverride)
            {
                SeenTimeoutOverride = timeoutOverride;
                return _built ?? throw new InvalidOperationException("CreateClient reached with an injected client.");
            }

            public string Fetch(PveSession session) => Invoke(session, c => c.Get("version"));
            public string FetchWithTimeout(PveSession session, TimeSpan timeout) => Invoke(session, timeout, c => c.Get("version"));
            public void Touch(PveSession session) => Invoke(session, c => { c.Get("version"); });
            public string Throw(PveSession session) => Invoke<string>(session, c => throw new InvalidOperationException("boom"));
        }

        [Fact]
        public void WithNoInjectedClient_TheClientBuiltForTheCallIsDisposed()
        {
            var built = new RecordingClient();
            var service = ProbeService.Building(built);

            service.Fetch(CreateSession());

            Assert.Equal(1, built.Gets);
            Assert.Equal(1, built.DisposeCalls);
        }

        [Fact]
        public void WithNoInjectedClient_TheClientIsDisposedWhenTheActionThrows()
        {
            var built = new RecordingClient();
            var service = ProbeService.Building(built);

            Assert.Throws<InvalidOperationException>(() => service.Throw(CreateSession()));

            Assert.Equal(1, built.DisposeCalls);
        }

        [Fact]
        public void WithAnInjectedClient_ItIsUsedAndNeverDisposed()
        {
            var injected = new RecordingClient();
            var service = ProbeService.Using(injected);

            service.Fetch(CreateSession());
            service.Touch(CreateSession());

            Assert.Equal(2, injected.Gets);
            Assert.Equal(0, injected.DisposeCalls);
        }

        [Fact]
        public void TheVoidOverloadDisposesTheClientItBuilt()
        {
            var built = new RecordingClient();
            var service = ProbeService.Building(built);

            service.Touch(CreateSession());

            Assert.Equal(1, built.DisposeCalls);
        }

        [Fact]
        public void TheTimeoutOverloadPassesTheOverrideToTheClientFactory()
        {
            var built = new RecordingClient();
            var service = ProbeService.Building(built);

            service.FetchWithTimeout(CreateSession(), TimeSpan.FromMinutes(30));

            Assert.Equal(TimeSpan.FromMinutes(30), service.SeenTimeoutOverride);
        }

        [Fact]
        public void ANullSessionIsRejectedBeforeAnyClientIsBuilt()
        {
            var built = new RecordingClient();
            var service = ProbeService.Building(built);

            var ex = Assert.Throws<ArgumentNullException>(() => service.Fetch(null!));

            Assert.Equal("session", ex.ParamName);
            Assert.Equal(0, built.DisposeCalls);
        }

        [Fact]
        public void NestedServicesShareTheInjectedClient()
        {
            var injected = new RecordingClient();

            new VmService(injected).GetVms(CreateSession());
            new ContainerService(injected).GetContainers(CreateSession());
            new TemplateService(injected).GetTemplates(CreateSession());

            Assert.Equal(3, injected.Gets);
            Assert.Equal(0, injected.DisposeCalls);
        }
    }
}
