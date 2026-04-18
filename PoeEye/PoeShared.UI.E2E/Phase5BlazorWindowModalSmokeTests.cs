using Microsoft.Playwright;
using Xunit;

namespace PoeShared.UI.E2E;

public sealed class Phase5BlazorWindowModalSmokeTests
{
    [Fact]
    public async Task ModalBlazorWindowRendersAndRespondsToClick()
    {
        await using var app = AvaloniaAppProcess.StartWindowHarness(GetFreePort(), BlazorWindowLaunchModes.Modal);
        await app.WaitForBrowserReadyAsync(TimeSpan.FromSeconds(60));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{app.DebugPort}");
        var page = await BlazorWindowHarness.WaitForWindowPageAsync(browser, TimeSpan.FromSeconds(30));

        await BlazorWindowHarness.AssertWindowParityAsync(page, "Dialog");
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
