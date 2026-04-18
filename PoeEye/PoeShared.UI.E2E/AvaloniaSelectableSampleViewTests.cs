using Microsoft.Playwright;
using Xunit;

namespace PoeShared.UI.E2E;

public sealed class AvaloniaSelectableSampleViewTests
{
    [Fact]
    public async Task CounterAltSampleViewRendersAndIncrementsByFive()
    {
        await using var app = AvaloniaAppProcess.StartBlazor(GetFreePort());
        await app.WaitForBrowserReadyAsync(TimeSpan.FromSeconds(60));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{app.DebugPort}");
        var page = await BlazorSampleViewHarness.WaitForSampleBrowserPageAsync(browser, TimeSpan.FromSeconds(30));
        await BlazorSampleViewHarness.SelectSampleViewAsync(page, AvaloniaSampleView.CounterAlt);

        await BlazorSampleViewHarness.AssertCounterAltAsync(page);
    }

    [Fact]
    public async Task CounterAltSampleViewLaunchArgumentSelectsAltViewImmediately()
    {
        await using var app = AvaloniaAppProcess.StartBlazor(GetFreePort(), AvaloniaSampleView.CounterAlt);
        await app.WaitForBrowserReadyAsync(TimeSpan.FromSeconds(60));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{app.DebugPort}");
        var page = await BlazorSampleViewHarness.WaitForSampleBrowserPageAsync(browser, TimeSpan.FromSeconds(30));

        await BlazorSampleViewHarness.AssertCounterAltAsync(page);
    }

    [Fact]
    public async Task SlowSampleViewEventuallyShowsCompletedInitializationState()
    {
        await using var app = AvaloniaAppProcess.StartBlazor(GetFreePort());
        await app.WaitForBrowserReadyAsync(TimeSpan.FromSeconds(60));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{app.DebugPort}");
        var page = await BlazorSampleViewHarness.WaitForSampleBrowserPageAsync(browser, TimeSpan.FromSeconds(30));
        await BlazorSampleViewHarness.SelectSampleViewAsync(page, AvaloniaSampleView.Slow);

        await BlazorSampleViewHarness.AssertSlowAsync(page);
    }

    [Fact]
    public async Task BrokenSampleViewShowsRecoveryUi()
    {
        await using var app = AvaloniaAppProcess.StartBlazor(GetFreePort());
        await app.WaitForBrowserReadyAsync(TimeSpan.FromSeconds(60));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{app.DebugPort}");
        var page = await BlazorSampleViewHarness.WaitForSampleBrowserPageAsync(browser, TimeSpan.FromSeconds(30));
        await BlazorSampleViewHarness.SelectSampleViewAsync(page, AvaloniaSampleView.Broken);

        await BlazorSampleViewHarness.AssertBrokenAsync(page);
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
