using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class VersionRoutes
{
    public static WebApplication MapVersionRoutes(this WebApplication app)
    {
        app.MapGet("/api2/json/version", () =>
            Results.Content(MockPveServer.GetFixture("pve9_version"), "application/json"));

        return app;
    }
}
