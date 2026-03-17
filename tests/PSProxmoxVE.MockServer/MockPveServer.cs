using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using PSProxmoxVE.MockServer.Routes;

namespace PSProxmoxVE.MockServer;

public record RequestRecord(string Method, string Path, string? Body, DateTime Timestamp);

public class MockPveServer : IDisposable
{
    private readonly WebApplication _app;
    private bool _disposed;

    public string BaseUrl { get; }
    public int Port { get; }
    public List<RequestRecord> Requests { get; } = new();

    private static readonly Dictionary<string, string> _fixtures = new();
    private static readonly object _fixtureLock = new();

    // Track task status calls per UPID for simulating running -> completed transitions
    internal Dictionary<string, int> TaskStatusCallCounts { get; } = new();

    private MockPveServer(WebApplication app, int port)
    {
        _app = app;
        Port = port;
        BaseUrl = $"https://localhost:{port}";
    }

    public static MockPveServer Start(int port = 0)
    {
        LoadFixtures();

        var cert = GenerateSelfSignedCert();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                listenOptions.UseHttps(cert);
            });
        });

        // Suppress default logging noise in tests
        builder.Logging.ClearProviders();

        var app = builder.Build();

        var server = new MockPveServer(app, port);

        // Request tracking middleware
        app.Use(async (context, next) =>
        {
            string? body = null;
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }
            server.Requests.Add(new RequestRecord(
                context.Request.Method,
                context.Request.Path + context.Request.QueryString,
                body,
                DateTime.UtcNow));
            await next();
        });

        // Auth middleware
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "";
            var method = context.Request.Method;

            // Skip auth for ticket creation and version endpoints
            bool skipAuth = (path == "/api2/json/access/ticket" && method == "POST")
                         || (path == "/api2/json/access/ticket" && method == "DELETE")
                         || (path == "/api2/json/version" && method == "GET");

            if (!skipAuth)
            {
                var hasCookie = context.Request.Headers["Cookie"].ToString().Contains("PVEAuthCookie=");
                var hasToken = context.Request.Headers["Authorization"].ToString().StartsWith("PVEAPIToken=");

                if (!hasCookie && !hasToken)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"data\":null,\"errors\":{\"authentication\":\"invalid credentials\"}}");
                    return;
                }
            }

            await next();
        });

        // Register all routes
        app.MapAuthRoutes();
        app.MapVersionRoutes();
        app.MapNodeRoutes();
        app.MapVmRoutes();
        app.MapContainerRoutes();
        app.MapStorageRoutes();
        app.MapNetworkRoutes();
        app.MapUserRoutes();
        app.MapClusterRoutes();
        app.MapTaskRoutes(server);
        app.MapSnapshotRoutes();

        app.StartAsync().GetAwaiter().GetResult();

        // Resolve the actual port (important when port=0 for random assignment)
        var addresses = app.Urls;
        var actualPort = port;
        var serverInstance = app.Services.GetRequiredService<IServer>();
        if (serverInstance != null)
        {
            var addressFeature = serverInstance.Features.Get<IServerAddressesFeature>();
            if (addressFeature != null)
            {
                var addr = addressFeature.Addresses.FirstOrDefault();
                if (addr != null)
                {
                    var uri = new Uri(addr);
                    actualPort = uri.Port;
                }
            }
        }

        return new MockPveServer(app, actualPort);
    }

    public void Reset()
    {
        Requests.Clear();
        TaskStatusCallCounts.Clear();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _app.StopAsync().GetAwaiter().GetResult();
            _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    public static string GetFixture(string name)
    {
        if (_fixtures.TryGetValue(name, out var content))
            return content;
        return "{\"data\":null}";
    }

    public static string GenerateUpid(string node, string type, int vmid = 0)
        => $"UPID:{node}:{Guid.NewGuid():N}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds():X}:{type}:{vmid}:root@pam:";

    private static X509Certificate2 GenerateSelfSignedCert()
    {
        var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=pve-mock", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
        return cert;
    }

    private static void LoadFixtures()
    {
        lock (_fixtureLock)
        {
            if (_fixtures.Count > 0) return;

            // Resolve fixtures path relative to the assembly location
            var basePath = AppContext.BaseDirectory;
            var fixturesPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "PSProxmoxVE.Core.Tests", "Fixtures"));

            // Fallback: try relative to the project source directory
            if (!Directory.Exists(fixturesPath))
            {
                fixturesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "PSProxmoxVE.Core.Tests", "Fixtures"));
            }

            if (!Directory.Exists(fixturesPath))
            {
                // Last resort: look from the working directory
                fixturesPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "PSProxmoxVE.Core.Tests", "Fixtures"));
            }

            if (Directory.Exists(fixturesPath))
            {
                foreach (var file in Directory.GetFiles(fixturesPath, "*.json"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    _fixtures[name] = File.ReadAllText(file);
                }
            }
        }
    }
}
