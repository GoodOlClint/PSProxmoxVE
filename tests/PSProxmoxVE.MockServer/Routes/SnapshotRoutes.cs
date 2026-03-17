using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class SnapshotRoutes
{
    public static WebApplication MapSnapshotRoutes(this WebApplication app)
    {
        app.MapGet("/api2/json/nodes/{node}/qemu/{vmid}/snapshot", (string node, int vmid) =>
            Results.Content(MockPveServer.GetFixture("pve9_snapshots"), "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/snapshot", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmsnapshot", vmid)}\"}}", "application/json"));

        app.MapDelete("/api2/json/nodes/{node}/qemu/{vmid}/snapshot/{snapname}", (string node, int vmid, string snapname) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmdelsnapshot", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/snapshot/{snapname}/rollback", (string node, int vmid, string snapname) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmrollback", vmid)}\"}}", "application/json"));

        return app;
    }
}
