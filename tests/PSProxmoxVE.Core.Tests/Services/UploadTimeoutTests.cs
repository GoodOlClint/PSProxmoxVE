using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using PSProxmoxVE.Core.Authentication;
using PSProxmoxVE.Core.Exceptions;
using PSProxmoxVE.Core.Services;
using Xunit;

namespace PSProxmoxVE.Core.Tests.Services
{
    /// <summary>
    /// Proves that <see cref="VmService.UploadOva"/> and <see cref="StorageService.UploadIso"/>
    /// apply their own <c>timeout</c> parameter to the <see cref="Client.PveHttpClient"/> they
    /// construct, instead of always inheriting <see cref="PveSession.Timeout"/>. Each test runs a
    /// real TLS server on loopback that completes the handshake and then never answers the HTTP
    /// request, so the request can only end via the client-side timeout.
    /// </summary>
    public class UploadTimeoutTests
    {
        private static X509Certificate2 CreateSelfSignedServerCert()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=127.0.0.1", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            // Re-import as PKCS12 — an ephemeral CertificateRequest key set is not usable by
            // SslStream's server-auth path on every platform without this round-trip.
            return new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
        }

        /// <summary>
        /// Starts a loopback TLS server that accepts one connection, completes the handshake,
        /// and then holds the connection open without ever writing an HTTP response.
        /// </summary>
        private static (TcpListener listener, int port) StartSilentTlsServer(X509Certificate2 cert, TimeSpan hold)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            _ = Task.Run(async () =>
            {
                try
                {
                    using var tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    using var sslStream = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: false);
                    await sslStream.AuthenticateAsServerAsync(cert, clientCertificateRequired: false,
                        checkCertificateRevocation: false).ConfigureAwait(false);
                    // Never read the request or write a response — the client is left waiting
                    // until its own HttpClient.Timeout fires, or until the test tears the
                    // listener down.
                    await Task.Delay(hold).ConfigureAwait(false);
                }
                catch
                {
                    // The test disposes the listener once it has its result; ignore the resulting
                    // teardown exceptions on this background task.
                }
            });

            return (listener, port);
        }

        private static PveSession NewSessionWithTimeout(int port, TimeSpan sessionTimeout)
        {
            var session = new PveSession("127.0.0.1", port, skipCertificateCheck: true,
                apiToken: "root@pam!test=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            session.Timeout = sessionTimeout;
            return session;
        }

        [Fact]
        public async Task UploadOva_ExplicitTimeout_FiresBeforeSessionDefault()
        {
            using var cert = CreateSelfSignedServerCert();
            var (listener, port) = StartSilentTlsServer(cert, TimeSpan.FromSeconds(60));
            var tempFile = Path.GetTempFileName();
            try
            {
                // Session default is 100s (PveSession's own built-in default); the explicit
                // override below must be what actually governs this call.
                var session = NewSessionWithTimeout(port, TimeSpan.FromSeconds(100));
                var service = new VmService();

                var sw = Stopwatch.StartNew();
                var ex = await Assert.ThrowsAsync<PveApiException>(() =>
                    Task.Run(() => service.UploadOva(session, "pve1", "local", tempFile,
                        timeout: TimeSpan.FromSeconds(1))));
                sw.Stop();

                Assert.Equal(HttpStatusCode.RequestTimeout, ex.StatusCode);
                Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30),
                    $"expected the 1s override to fire, not the 100s session default; took {sw.Elapsed}");
            }
            finally
            {
                listener.Stop();
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task UploadIso_ExplicitTimeout_FiresBeforeSessionDefault()
        {
            using var cert = CreateSelfSignedServerCert();
            var (listener, port) = StartSilentTlsServer(cert, TimeSpan.FromSeconds(60));
            var tempFile = Path.GetTempFileName();
            try
            {
                var session = NewSessionWithTimeout(port, TimeSpan.FromSeconds(100));
                var service = new StorageService();

                var sw = Stopwatch.StartNew();
                var ex = await Assert.ThrowsAsync<PveApiException>(() =>
                    Task.Run(() => service.UploadIso(session, "pve1", "local", tempFile,
                        progressCallback: null, timeout: TimeSpan.FromSeconds(1))));
                sw.Stop();

                Assert.Equal(HttpStatusCode.RequestTimeout, ex.StatusCode);
                Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30),
                    $"expected the 1s override to fire, not the 100s session default; took {sw.Elapsed}");
            }
            finally
            {
                listener.Stop();
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task UploadOva_NoTimeoutGiven_UsesThirtyMinuteDefaultNotSessionTimeout()
        {
            using var cert = CreateSelfSignedServerCert();
            var (listener, port) = StartSilentTlsServer(cert, TimeSpan.FromSeconds(10));
            var tempFile = Path.GetTempFileName();
            try
            {
                // Session default is deliberately shorter than the wait window below. If
                // UploadOva let the session's own timeout govern the request (the #139 bug),
                // this would throw within that window instead of still being in flight.
                var session = NewSessionWithTimeout(port, TimeSpan.FromMilliseconds(300));
                var service = new VmService();

                var uploadTask = Task.Run(() => service.UploadOva(session, "pve1", "local", tempFile));
                var completedFirst = await Task.WhenAny(uploadTask, Task.Delay(TimeSpan.FromSeconds(3)))
                    .ConfigureAwait(false);

                Assert.NotSame(uploadTask, completedFirst);
            }
            finally
            {
                listener.Stop();
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task UploadIso_NoTimeoutGiven_UsesThirtyMinuteDefaultNotSessionTimeout()
        {
            using var cert = CreateSelfSignedServerCert();
            var (listener, port) = StartSilentTlsServer(cert, TimeSpan.FromSeconds(10));
            var tempFile = Path.GetTempFileName();
            try
            {
                var session = NewSessionWithTimeout(port, TimeSpan.FromMilliseconds(300));
                var service = new StorageService();

                var uploadTask = Task.Run(() => service.UploadIso(session, "pve1", "local", tempFile));
                var completedFirst = await Task.WhenAny(uploadTask, Task.Delay(TimeSpan.FromSeconds(3)))
                    .ConfigureAwait(false);

                Assert.NotSame(uploadTask, completedFirst);
            }
            finally
            {
                listener.Stop();
                File.Delete(tempFile);
            }
        }
    }
}
