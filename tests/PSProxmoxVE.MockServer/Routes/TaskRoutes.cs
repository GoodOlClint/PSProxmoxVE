using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class TaskRoutes
{
    public static WebApplication MapTaskRoutes(this WebApplication app, MockPveServer server)
    {
        app.MapGet("/api2/json/nodes/{node}/tasks/{upid}/status", (string node, string upid) =>
        {
            // Track call count per UPID: first call returns running, subsequent return completed
            int count;
            lock (server.TaskStatusCallCounts)
            {
                if (!server.TaskStatusCallCounts.TryGetValue(upid, out count))
                    count = 0;
                count++;
                server.TaskStatusCallCounts[upid] = count;
            }

            if (count <= 1)
                return Results.Content(MockPveServer.GetFixture("pve9_task_running"), "application/json");
            else
                return Results.Content(MockPveServer.GetFixture("pve9_tasks"), "application/json");
        });

        app.MapGet("/api2/json/nodes/{node}/tasks/{upid}/log", (string node, string upid) =>
            Results.Content("{\"data\":[{\"n\":1,\"t\":\"starting task\"}]}", "application/json"));

        return app;
    }
}
