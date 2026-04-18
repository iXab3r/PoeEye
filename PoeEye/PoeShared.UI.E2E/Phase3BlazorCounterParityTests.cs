using Microsoft.Playwright;
using Xunit;

namespace PoeShared.UI.E2E;

public sealed class Phase3BlazorCounterParityTests
{
    [Fact]
    public async Task BlazorCounterPageShowsParitySignalsAndUpdatesState()
    {
        await using var app = AvaloniaAppProcess.StartBlazor(GetFreePort());
        await app.WaitForBrowserReadyAsync(TimeSpan.FromSeconds(60));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{app.DebugPort}");
        var page = await BlazorCounterHarness.WaitForPageAsync(browser, TimeSpan.FromSeconds(30));

        await BlazorCounterHarness.AssertParityCounterBehaviorAsync(page);
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
