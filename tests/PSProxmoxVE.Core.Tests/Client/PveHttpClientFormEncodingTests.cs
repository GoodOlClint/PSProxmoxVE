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
    public class PveHttpClientFormEncodingTests
    {
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

        private static (PveHttpClient client, CapturingHandler handler) NewCapturingClient()
        {
            var client = new PveHttpClient(NewSession());
            var handler = new CapturingHandler();
            SetInnerHttpClient(client, new HttpClient(handler));
            return (client, handler);
        }

        [Fact]
        public async Task PostAsync_SemicolonInValue_IsPercentEncoded()
        {
            var (client, handler) = NewCapturingClient();
            using (client)
            {
                await client.PostAsync("nodes/pve/qemu/100/config",
                    new Dictionary<string, string> { ["boot"] = "order=scsi0;ide2" });
            }

            // PVE treats a raw ';' as a form-field separator, so it must be encoded.
            Assert.Contains("boot=order%3Dscsi0%3Bide2", handler.LastBody);
            Assert.DoesNotContain(";", handler.LastBody);
        }

        [Fact]
        public async Task PutAsync_SemicolonInValue_IsPercentEncoded()
        {
            var (client, handler) = NewCapturingClient();
            using (client)
            {
                await client.PutAsync("nodes/pve/qemu/100/config",
                    new Dictionary<string, string> { ["boot"] = "order=ide2;virtio0;net0" });
            }

            Assert.Contains("boot=order%3Dide2%3Bvirtio0%3Bnet0", handler.LastBody);
            Assert.DoesNotContain(";", handler.LastBody);
        }

        [Fact]
        public async Task PostAsync_CommaAndColonInValue_RemainLiteral()
        {
            var (client, handler) = NewCapturingClient();
            using (client)
            {
                // Drive options use literal commas; cluster-join values use literal colons.
                // Minimal-encoding policy must keep both unescaped.
                await client.PostAsync("nodes/pve/qemu/100/config",
                    new Dictionary<string, string> { ["ide2"] = "nas:iso/win.iso,media=cdrom" });
            }

            Assert.Contains("ide2=nas:iso/win.iso,media%3Dcdrom", handler.LastBody);
        }

        [Fact]
        public async Task PostAsync_RepeatedKeys_EmitOneFieldPerValue()
        {
            var (client, handler) = NewCapturingClient();
            using (client)
            {
                // PVE array params (e.g. guest-exec command) are sent as repeated keys.
                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("command", "cmd.exe"),
                    new KeyValuePair<string, string>("command", "/c"),
                    new KeyValuePair<string, string>("command", "echo"),
                    new KeyValuePair<string, string>("command", "WLMARK42"),
                };
                await client.PostAsync("nodes/pve/qemu/100/agent/exec", data);
            }

            Assert.Equal("command=cmd.exe&command=/c&command=echo&command=WLMARK42", handler.LastBody);
        }

        [Fact]
        public async Task PostAsync_RepeatedKeys_EncodeEachValueIndependently()
        {
            var (client, handler) = NewCapturingClient();
            using (client)
            {
                var data = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("command", "powershell.exe"),
                    new KeyValuePair<string, string>("command", "-Command"),
                    new KeyValuePair<string, string>("command", "echo a=b;c"),
                };
                await client.PostAsync("nodes/pve/qemu/100/agent/exec", data);
            }

            // The '=' and ';' inside an arg must be encoded so they don't split the body,
            // while spaces follow the form convention ('+').
            Assert.Equal("command=powershell.exe&command=-Command&command=echo+a%3Db%3Bc", handler.LastBody);
        }

        private sealed class CapturingHandler : HttpMessageHandler
        {
            public string LastBody { get; private set; } = string.Empty;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastBody = request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":null}")
                };
            }
        }
    }
}
