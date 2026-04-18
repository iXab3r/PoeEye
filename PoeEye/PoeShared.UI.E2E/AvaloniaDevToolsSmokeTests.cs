using Xunit;
using Microsoft.Playwright;

namespace PoeShared.UI.E2E;

public sealed class AvaloniaDevToolsSmokeTests
{
    [Fact]
    public async Task DevToolsButtonOpensBrowserDevToolsTarget()
    {
        var existingDevToolsWindows = NativeWindowInterop.GetVisibleTopLevelWindows()
            .Where(IsDevToolsWindow)
            .ToArray();
        foreach (var window in existingDevToolsWindows)
        {
            NativeWindowInterop.CloseWindow(window.Handle);
        }

        await Task.Delay(500);

        await using var app = AvaloniaAppProcess.StartBlazor(GetFreePort());
        await app.WaitForBrowserReadyAsync(TimeSpan.FromSeconds(60));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{app.DebugPort}");
        var page = await BlazorSampleViewHarness.WaitForSampleBrowserPageAsync(browser, TimeSpan.FromSeconds(30));
        await page.Locator("[data-testid='open-devtools-button']").ClickAsync();

        var existingHandles = existingDevToolsWindows
            .Select(x => x.Handle)
            .ToHashSet();
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var newDevToolsWindows = NativeWindowInterop.GetVisibleTopLevelWindows()
                .Where(IsDevToolsWindow)
                .Where(x => !existingHandles.Contains(x.Handle))
                .ToArray();
            if (newDevToolsWindows.Any(IsWebView2DevToolsWindow))
            {
                return;
            }

            await Task.Delay(250);
        }

        var windowsDump = string.Join(
            " | ",
            NativeWindowInterop.GetVisibleTopLevelWindows().Select(x => $"{x.ProcessName}:{x.Title}"));
        throw new Xunit.Sdk.XunitException($"Connected WebView2 DevTools window did not appear. Windows={windowsDump}");
    }

    private static bool IsDevToolsWindow(NativeWindowInterop.TopLevelWindowInfo window)
    {
        return window.Title.Contains("devtools", StringComparison.OrdinalIgnoreCase) ||
               window.Title.Contains("developer tools", StringComparison.OrdinalIgnoreCase) ||
               window.ProcessName.Equals("msedge", StringComparison.OrdinalIgnoreCase) && window.Title.Contains("DevTools", StringComparison.OrdinalIgnoreCase) ||
               window.ProcessName.Equals("msedgewebview2", StringComparison.OrdinalIgnoreCase) && window.Title.Contains("DevTools", StringComparison.OrdinalIgnoreCase) ||
               window.ProcessName.Equals("chrome", StringComparison.OrdinalIgnoreCase) && window.Title.Contains("DevTools", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWebView2DevToolsWindow(NativeWindowInterop.TopLevelWindowInfo window)
    {
        return IsDevToolsWindow(window) &&
               window.ProcessName.Equals("msedgewebview2", StringComparison.OrdinalIgnoreCase);
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
