using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class StorageRoutes
{
    public static WebApplication MapStorageRoutes(this WebApplication app)
    {
        app.MapGet("/api2/json/storage", () =>
            Results.Content(MockPveServer.GetFixture("pve9_storage"), "application/json"));

        app.MapGet("/api2/json/nodes/{node}/storage/{storage}/content", (string node, string storage) =>
            Results.Content(MockPveServer.GetFixture("pve9_storage_content"), "application/json"));

        app.MapPost("/api2/json/nodes/{node}/storage/{storage}/upload", (string node, string storage) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "upload")}\"}}", "application/json"))
            .DisableAntiforgery();

        app.MapPost("/api2/json/storage", () =>
            Results.Content("{\"data\":null}", "application/json"));

        app.MapDelete("/api2/json/storage/{storage}", (string storage) =>
            Results.Content("{\"data\":null}", "application/json"));

        return app;
    }
}
