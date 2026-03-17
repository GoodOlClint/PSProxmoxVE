using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class ContainerRoutes
{
    public static WebApplication MapContainerRoutes(this WebApplication app)
    {
        app.MapGet("/api2/json/nodes/{node}/lxc", (string node) =>
            Results.Content(MockPveServer.GetFixture("pve9_containers"), "application/json"));

        app.MapGet("/api2/json/nodes/{node}/lxc/{vmid}/config", (string node, int vmid) =>
            Results.Content("{\"data\":{\"hostname\":\"test\",\"memory\":512,\"cores\":1}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/lxc", (string node) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "vzcreate")}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/lxc/{vmid}/status/start", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "vzstart", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/lxc/{vmid}/status/stop", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "vzstop", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/lxc/{vmid}/status/shutdown", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "vzshutdown", vmid)}\"}}", "application/json"));

        app.MapPut("/api2/json/nodes/{node}/lxc/{vmid}/config", (string node, int vmid) =>
            Results.Content("{\"data\":null}", "application/json"));

        app.MapDelete("/api2/json/nodes/{node}/lxc/{vmid}", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "vzdestroy", vmid)}\"}}", "application/json"));

        return app;
    }
}
