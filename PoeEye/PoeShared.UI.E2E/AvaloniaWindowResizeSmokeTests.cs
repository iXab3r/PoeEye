using Microsoft.Playwright;
using Xunit;

namespace PoeShared.UI.E2E;

public sealed class AvaloniaWindowResizeSmokeTests
{
    [Fact]
    public async Task MainWindowCanResizeFromWindowEdge()
    {
        await using var app = AvaloniaAppProcess.StartBlazor(GetFreePort());
        await app.WaitForBrowserReadyAsync(TimeSpan.FromSeconds(60));
        var hwnd = await app.WaitForMainWindowHandleAsync(TimeSpan.FromSeconds(15));
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.ConnectOverCDPAsync($"http://127.0.0.1:{app.DebugPort}");
        var page = await BlazorSampleViewHarness.WaitForSampleBrowserPageAsync(browser, TimeSpan.FromSeconds(30));
        var appHandleHex = await page.Locator("[data-testid='window-handle']").InnerTextAsync();
        Assert.Equal($"0x{hwnd.ToInt64():X}", appHandleHex);
        var before = NativeWindowInterop.GetWindowRect(hwnd);
        var windowStyle = NativeWindowInterop.GetWindowStyle(hwnd);
        await page.Locator("[data-testid='grow-window-button']").DispatchEventAsync("click");
        await Task.Delay(600);
        var afterProgrammaticGrow = NativeWindowInterop.GetWindowRect(hwnd);
        var boundsApplyStatus = await page.Locator("[data-testid='bounds-apply-status']").InnerTextAsync();
        var programmaticGrowWorked = afterProgrammaticGrow.Width > before.Width && afterProgrammaticGrow.Height > before.Height;
        var nativeHitTargetsPresent = NativeWindowInterop.HasResizeHitTargets(hwnd);
        var nativeResizeWorked = await NativeWindowInterop.TryResizeBottomRightAsync(hwnd, 160, 120);
        var afterNativeEdgeDrag = NativeWindowInterop.GetWindowRect(hwnd);
        var grip = page.Locator("[data-testid='window-resize-grip']");
        await grip.WaitForAsync();
        await grip.HoverAsync();
        await page.Mouse.DownAsync();
        await Task.Delay(150);
        var resizeStatus = await page.Locator("[data-testid='resize-status']").InnerTextAsync();
        var statusParts = resizeStatus.Split(':');
        Assert.True(statusParts.Length == 2, $"Unexpected resize status payload: {resizeStatus}");
        var pointParts = statusParts[1].Split(',');
        Assert.True(pointParts.Length == 2, $"Unexpected resize status point payload: {resizeStatus}");
        var startX = int.Parse(pointParts[0]);
        var startY = int.Parse(pointParts[1]);
        await NativeWindowInterop.DragMouseAsync(startX, startY, startX + 160, startY + 120);
        await Task.Delay(900);
        var after = NativeWindowInterop.GetWindowRect(hwnd);
        Assert.True(
            after.Width > afterProgrammaticGrow.Width && after.Height > afterProgrammaticGrow.Height,
            $"Expected grip-initiated native resize to grow the window, but before={before.Width}x{before.Height}, afterGrow={afterProgrammaticGrow.Width}x{afterProgrammaticGrow.Height}, afterNative={afterNativeEdgeDrag.Width}x{afterNativeEdgeDrag.Height}, afterDrag={after.Width}x{after.Height}, bounds={boundsApplyStatus}, style=0x{windowStyle.ToInt64():X}, growWorked={programmaticGrowWorked}, nativeHitTargetsPresent={nativeHitTargetsPresent}, nativeResizeWorked={nativeResizeWorked}, status={resizeStatus}.");
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
