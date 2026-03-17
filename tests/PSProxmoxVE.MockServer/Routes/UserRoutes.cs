using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class UserRoutes
{
    public static WebApplication MapUserRoutes(this WebApplication app)
    {
        app.MapGet("/api2/json/access/users", () =>
            Results.Content(MockPveServer.GetFixture("pve9_users"), "application/json"));

        app.MapGet("/api2/json/access/users/{userid}", (string userid) =>
        {
            // Return first user from fixture
            var fixture = MockPveServer.GetFixture("pve9_users");
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(fixture);
                var dataArray = doc.RootElement.GetProperty("data");
                var first = dataArray[0];
                return Results.Content($"{{\"data\":{first.GetRawText()}}}", "application/json");
            }
            catch
            {
                return Results.Content("{\"data\":null}", "application/json");
            }
        });

        app.MapPost("/api2/json/access/users", () =>
            Results.Content("{\"data\":null}", "application/json"));

        app.MapPut("/api2/json/access/users/{userid}", (string userid) =>
            Results.Content("{\"data\":null}", "application/json"));

        app.MapDelete("/api2/json/access/users/{userid}", (string userid) =>
            Results.Content("{\"data\":null}", "application/json"));

        // Roles
        app.MapGet("/api2/json/access/roles", () =>
            Results.Content(MockPveServer.GetFixture("pve9_roles"), "application/json"));

        app.MapPost("/api2/json/access/roles", () =>
            Results.Content("{\"data\":null}", "application/json"));

        app.MapDelete("/api2/json/access/roles/{roleid}", (string roleid) =>
            Results.Content("{\"data\":null}", "application/json"));

        // ACL
        app.MapGet("/api2/json/access/acl", () =>
            Results.Content("{\"data\":[]}", "application/json"));

        app.MapPut("/api2/json/access/acl", () =>
            Results.Content("{\"data\":null}", "application/json"));

        return app;
    }
}
