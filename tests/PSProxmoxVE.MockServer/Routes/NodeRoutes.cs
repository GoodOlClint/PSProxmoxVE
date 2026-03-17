using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class NodeRoutes
{
    public static WebApplication MapNodeRoutes(this WebApplication app)
    {
        app.MapGet("/api2/json/nodes", () =>
            Results.Content(MockPveServer.GetFixture("pve9_nodes"), "application/json"));

        app.MapGet("/api2/json/nodes/{node}/status", (string node) =>
            Results.Content(MockPveServer.GetFixture("pve9_node_status"), "application/json"));

        return app;
    }
}
