using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class VmRoutes
{
    public static WebApplication MapVmRoutes(this WebApplication app)
    {
        app.MapGet("/api2/json/nodes/{node}/qemu", (string node) =>
            Results.Content(MockPveServer.GetFixture("pve9_vms"), "application/json"));

        app.MapGet("/api2/json/nodes/{node}/qemu/{vmid}/config", (string node, int vmid) =>
            Results.Content(MockPveServer.GetFixture("pve9_vm_config"), "application/json"));

        app.MapGet("/api2/json/nodes/{node}/qemu/{vmid}/status/current", (string node, int vmid) =>
        {
            // Return first VM from fixture as a single-item response
            var fixture = MockPveServer.GetFixture("pve9_vms");
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(fixture);
                var dataArray = doc.RootElement.GetProperty("data");
                foreach (var item in dataArray.EnumerateArray())
                {
                    if (item.TryGetProperty("vmid", out var id) && id.GetInt32() == vmid)
                        return Results.Content($"{{\"data\":{item.GetRawText()}}}", "application/json");
                }
                // If no match, return first item
                var first = dataArray[0];
                return Results.Content($"{{\"data\":{first.GetRawText()}}}", "application/json");
            }
            catch
            {
                return Results.Content("{\"data\":null}", "application/json");
            }
        });

        app.MapPost("/api2/json/nodes/{node}/qemu", (string node) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmcreate")}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/status/start", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmstart", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/status/stop", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmstop", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/status/shutdown", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmshutdown", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/status/reset", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmreset", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/status/suspend", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmsuspend", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/status/resume", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmresume", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/clone", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmclone", vmid)}\"}}", "application/json"));

        app.MapPost("/api2/json/nodes/{node}/qemu/{vmid}/migrate", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmigrate", vmid)}\"}}", "application/json"));

        app.MapPut("/api2/json/nodes/{node}/qemu/{vmid}/config", (string node, int vmid) =>
            Results.Content("{\"data\":null}", "application/json"));

        app.MapPut("/api2/json/nodes/{node}/qemu/{vmid}/resize", (string node, int vmid) =>
            Results.Content("{\"data\":null}", "application/json"));

        app.MapDelete("/api2/json/nodes/{node}/qemu/{vmid}", (string node, int vmid) =>
            Results.Content($"{{\"data\":\"{MockPveServer.GenerateUpid(node, "qmdestroy", vmid)}\"}}", "application/json"));

        return app;
    }
}
