using Microsoft.Playwright;
using Xunit;

namespace PoeShared.UI.E2E;

public sealed class AvaloniaVisualSurveyTests
{
    [Fact]
    public async Task ShouldCaptureSampleBrowserAndWindowScreenshots()
    {
        var outputDirectory = CreateOutputDirectory();
        Console.WriteLine($"Avalonia visual survey output: {outputDirectory}");

        await using var app = AvaloniaAppProcess.StartBlazor(GetFreePort());
        await app.WaitForBrowserReadyAsync(TimeSpan.FromSeconds(60));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{app.DebugPort}");
        var page = await BlazorSampleViewHarness.WaitForSampleBrowserPageAsync(browser, TimeSpan.FromSeconds(30));

        await page.SetViewportSizeAsync(1600, 1100);
        await page.Locator("[data-testid='sample-browser-root']").WaitForAsync();

        await CaptureLocatorScreenshotAsync(
            page.Locator("[data-testid='sample-browser-root']"),
            Path.Combine(outputDirectory, "sample-browser-counter.png"));

        await BlazorSampleViewHarness.SelectSampleViewAsync(page, AvaloniaSampleView.CounterAlt);
        await CaptureLocatorScreenshotAsync(
            page.Locator("[data-testid='sample-browser-root']"),
            Path.Combine(outputDirectory, "sample-browser-counter-alt.png"));

        await BlazorSampleViewHarness.SelectSampleViewAsync(page, AvaloniaSampleView.Slow);
        await page.Locator("[data-testid='slow-status']").WaitForAsync();
        await CaptureLocatorScreenshotAsync(
            page.Locator("[data-testid='sample-browser-root']"),
            Path.Combine(outputDirectory, "sample-browser-slow.png"));

        await BlazorSampleViewHarness.SelectSampleViewAsync(page, AvaloniaSampleView.Broken);
        await CaptureLocatorScreenshotAsync(
            page.Locator("[data-testid='sample-browser-root']"),
            Path.Combine(outputDirectory, "sample-browser-broken.png"));

        await page.ClickAsync("[data-testid='open-window-button']");
        var windowPage = await BlazorWindowHarness.WaitForWindowPageAsync(browser, TimeSpan.FromSeconds(30));
        await windowPage.SetViewportSizeAsync(1200, 900);
        await windowPage.Locator(BlazorWindowPage.ShellSelector).WaitForAsync();
        await CaptureLocatorScreenshotAsync(
            windowPage.Locator(BlazorWindowPage.ShellSelector),
            Path.Combine(outputDirectory, "modeless-window-shell.png"));

        Assert.True(File.Exists(Path.Combine(outputDirectory, "sample-browser-counter.png")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "sample-browser-counter-alt.png")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "sample-browser-slow.png")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "sample-browser-broken.png")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "modeless-window-shell.png")));
    }

    private static async Task CaptureLocatorScreenshotAsync(ILocator locator, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await locator.ScreenshotAsync(new LocatorScreenshotOptions
        {
            Path = path
        });
        Console.WriteLine($"Captured screenshot: {path}");
    }

    private static string CreateOutputDirectory()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "PoeShared.UI.E2E",
            "AvaloniaVisualSurvey",
            timestamp);
        Directory.CreateDirectory(outputDirectory);
        return outputDirectory;
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
