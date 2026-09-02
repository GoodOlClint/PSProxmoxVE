using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Client;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Client
{
    public class PveHttpClientPathEscapingTests
    {
        private static void SetInnerHttpClient(PveHttpClient client, HttpClient newInner)
        {
            var field = typeof(PveHttpClient).GetField("_httpClient",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            ((HttpClient)field.GetValue(client)!).Dispose();
            field.SetValue(client, newInner);
        }

        private static (PveHttpClient client, ScriptedHandler handler) NewClient(
            params (HttpStatusCode status, string body)[] responses)
        {
            var session = new PveSession("pve.example.com", 8006, false,
                "root@pam!token=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var client = new PveHttpClient(session);
            var handler = new ScriptedHandler(responses);
            SetInnerHttpClient(client, new HttpClient(handler));
            return (client, handler);
        }

        [Fact]
        public async Task DeleteAsync_WithEscapedPathSegment_DoesNotCollapseAcrossTheRealUriParser()
        {
            var maliciousName = "../access/users/root@pam!t";
            var (client, handler) = NewClient(
                (HttpStatusCode.OK, "{\"data\":null}"));

            using (client)
            {
                await client.DeleteAsync($"storage/{Uri.EscapeDataString(maliciousName)}");
            }

            Assert.Single(handler.Uris);
            var uri = handler.Uris[0];
            Assert.Contains("storage/..%2Faccess", uri);
            Assert.DoesNotContain("storage/../", uri);
        }

        private sealed class ScriptedHandler : HttpMessageHandler
        {
            private readonly (HttpStatusCode status, string body)[] _responses;
            private int _index;

            public List<string> Uris { get; } = new List<string>();

            public ScriptedHandler((HttpStatusCode status, string body)[] responses)
            {
                _responses = responses;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Uris.Add(request.RequestUri!.ToString());

                if (_index >= _responses.Length)
                    throw new InvalidOperationException("ScriptedHandler ran out of responses.");

                var (status, body) = _responses[_index++];
                return new HttpResponseMessage(status) { Content = new StringContent(body) };
            }
        }
    }
}
