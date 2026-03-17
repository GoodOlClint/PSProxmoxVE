using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class NetworkRoutes
{
    public static WebApplication MapNetworkRoutes(this WebApplication app)
    {
        app.MapGet("/api2/json/nodes/{node}/network", (string node) =>
            Results.Content(MockPveServer.GetFixture("pve9_networks"), "application/json"));

        app.MapPost("/api2/json/nodes/{node}/network", (string node) =>
            Results.Content("{\"data\":null}", "application/json"));

        app.MapPut("/api2/json/nodes/{node}/network/{iface}", (string node, string iface) =>
            Results.Content("{\"data\":null}", "application/json"));

        app.MapDelete("/api2/json/nodes/{node}/network/{iface}", (string node, string iface) =>
            Results.Content("{\"data\":null}", "application/json"));

        // Apply network config
        app.MapPut("/api2/json/nodes/{node}/network", (string node) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "netapply")}\"}}", "application/json"));

        // SDN routes
        app.MapGet("/api2/json/cluster/sdn/zones", () =>
            Results.Content(MockPveServer.GetFixture("pve9_sdn_zones"), "application/json"));

        app.MapGet("/api2/json/cluster/sdn/vnets", () =>
            Results.Content(MockPveServer.GetFixture("pve9_sdn_vnets"), "application/json"));

        return app;
    }
}
