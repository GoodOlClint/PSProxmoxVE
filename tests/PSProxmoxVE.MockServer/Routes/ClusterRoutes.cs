using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PSProxmoxVE.MockServer.Routes;

public static class ClusterRoutes
{
    public static WebApplication MapClusterRoutes(this WebApplication app)
    {
        app.MapGet("/api2/json/cluster/status", () =>
            Results.Content(MockPveServer.GetFixture("pve9_cluster_status"), "application/json"));

        return app;
    }
}
