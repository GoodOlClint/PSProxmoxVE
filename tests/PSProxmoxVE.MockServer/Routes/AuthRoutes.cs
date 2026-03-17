using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class AuthRoutes
{
    public static WebApplication MapAuthRoutes(this WebApplication app)
    {
        app.MapPost("/api2/json/access/ticket", () =>
            Results.Content(MockPveServer.GetFixture("pve9_ticket_response"), "application/json"));

        app.MapDelete("/api2/json/access/ticket", () =>
            Results.Content("{\"data\":null}", "application/json"));

        return app;
    }
}
