using PSProxmoxVE.MockServer;

// When run standalone, start on a fixed port
using var server = MockPveServer.Start(port: 8006);
Console.WriteLine($"Mock Proxmox VE API server running at {server.BaseUrl}");
Console.WriteLine("Press Ctrl+C to stop.");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    // Graceful shutdown
}
