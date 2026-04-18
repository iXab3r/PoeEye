using System.Collections.Concurrent;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace PoeShared.UI.E2E;

internal static class BlazorWindowHarness
{
    public static Task<IPage> WaitForWindowPageAsync(IBrowser browser, TimeSpan timeout)
        => BrowserPageDiscovery.WaitForPageWithSelectorAsync(browser, BlazorWindowPage.RootSelector, timeout);

    public static Task AssertWindowInteractiveAsync(IPage page, string expectedKind)
        => AssertWindowFlowAsync(page, expectedKind);

    public static Task AssertWindowParityAsync(IPage page, string expectedKind)
        => AssertWindowFlowAsync(page, expectedKind);

    private static async Task AssertWindowFlowAsync(IPage page, string expectedKind)
    {
        var diagnostics = new WindowDiagnostics(page);
        diagnostics.Attach();
        try
        {
            await page.Locator(BlazorWindowPage.ShellSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.TitleBarSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.TitleBarTextSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.TitleBarDragRegionSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.TitleBarMinimizeButtonSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.TitleBarMaximizeButtonSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.TitleBarCloseButtonSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.RootSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.BodySelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.TitleSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.CounterSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.IncrementButtonSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.KindSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.DisplayNameSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.InstanceIdSelector).WaitForAsync();
            await page.Locator(BlazorWindowPage.CloseButtonSelector).WaitForAsync();

            await AssertShellStructureAsync(page);
            await AssertShellAutomationMetadataAsync(page, expectedKind);

            var titleBarText = await page.Locator(BlazorWindowPage.TitleBarTextSelector).InnerTextAsync();
            Assert.Contains(expectedKind, titleBarText, StringComparison.OrdinalIgnoreCase);

            var title = await page.Locator(BlazorWindowPage.TitleSelector).InnerTextAsync();
            Assert.Equal(title, titleBarText);
            Assert.Contains(expectedKind, title, StringComparison.OrdinalIgnoreCase);

            var kind = await page.Locator(BlazorWindowPage.KindSelector).InnerTextAsync();
            Assert.Equal(expectedKind, kind);

            var counter = await page.Locator(BlazorWindowPage.CounterSelector).InnerTextAsync();
            Assert.Equal("0", counter);

            var displayName = await page.Locator(BlazorWindowPage.DisplayNameSelector).InnerTextAsync();
            Assert.Contains(expectedKind, displayName, StringComparison.OrdinalIgnoreCase);

            var instanceId = await page.Locator(BlazorWindowPage.InstanceIdSelector).InnerTextAsync();
            Assert.False(string.IsNullOrWhiteSpace(instanceId));

            diagnostics.RecordSnapshot("before-click");
            await page.Locator(BlazorWindowPage.IncrementButtonSelector).ClickAsync();
            diagnostics.RecordSnapshot("after-click");

            await WaitForCounterChangeAsync(page, diagnostics, counter, TimeSpan.FromSeconds(5));

            var updatedCounter = await page.Locator(BlazorWindowPage.CounterSelector).InnerTextAsync();
            Assert.Equal("1", updatedCounter);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(diagnostics.BuildFailureMessage($"{expectedKind} window flow", ex), ex);
        }
        finally
        {
            diagnostics.Detach();
        }
    }

    private static async Task AssertShellStructureAsync(IPage page)
    {
        var shell = page.Locator(BlazorWindowPage.ShellSelector);
        var titleBar = page.Locator(BlazorWindowPage.TitleBarSelector);
        var body = page.Locator(BlazorWindowPage.BodySelector);

        Assert.Equal("SECTION", await GetTagNameAsync(shell));
        Assert.Equal("HEADER", await GetTagNameAsync(titleBar));
        Assert.Equal("SECTION", await GetTagNameAsync(body));

        Assert.Equal(BlazorWindowPage.ShellId, await GetRequiredAttributeAsync(shell, "id"));
        Assert.Equal(BlazorWindowPage.TitleBarId, await GetRequiredAttributeAsync(titleBar, "id"));
        Assert.Equal(BlazorWindowPage.BodyId, await GetRequiredAttributeAsync(body, "id"));

        var shellClass = await GetRequiredAttributeAsync(shell, "class");
        Assert.Contains(BlazorWindowPage.ShellClassName, shellClass, StringComparison.Ordinal);

        Assert.Equal("Drag window", await GetRequiredAttributeAsync(page.Locator(BlazorWindowPage.TitleBarDragRegionSelector), "title"));
        Assert.Equal("Minimize", await GetRequiredAttributeAsync(page.Locator(BlazorWindowPage.TitleBarMinimizeButtonSelector), "title"));
        Assert.Equal("Toggle maximize", await GetRequiredAttributeAsync(page.Locator(BlazorWindowPage.TitleBarMaximizeButtonSelector), "title"));
        Assert.Equal("Close", await GetRequiredAttributeAsync(page.Locator(BlazorWindowPage.TitleBarCloseButtonSelector), "title"));

        var isAlignedShellLayout = await page.EvaluateAsync<bool>(
            @"selectors => {
                const shell = document.querySelector(selectors.shell);
                const titleBar = document.querySelector(selectors.titleBar);
                const body = document.querySelector(selectors.body);
                return !!shell &&
                       !!titleBar &&
                       !!body &&
                       titleBar.parentElement === shell &&
                       body.parentElement === shell &&
                       shell.firstElementChild === titleBar &&
                       titleBar.nextElementSibling === body;
            }",
            new
            {
                shell = BlazorWindowPage.ShellSelector,
                titleBar = BlazorWindowPage.TitleBarSelector,
                body = BlazorWindowPage.BodySelector
            });
        Assert.True(isAlignedShellLayout, "Expected shell to contain titlebar followed by body.");
    }

    private static async Task AssertShellAutomationMetadataAsync(IPage page, string expectedKind)
    {
        var shell = page.Locator(BlazorWindowPage.ShellSelector);
        var titleBar = page.Locator(BlazorWindowPage.TitleBarSelector);
        var body = page.Locator(BlazorWindowPage.BodySelector);

        var shellWindowId = await GetRequiredAttributeAsync(shell, BlazorWindowPage.WindowIdAttribute);
        var titleBarWindowId = await GetRequiredAttributeAsync(titleBar, BlazorWindowPage.WindowIdAttribute);
        var bodyWindowId = await GetRequiredAttributeAsync(body, BlazorWindowPage.WindowIdAttribute);
        Assert.Equal(shellWindowId, titleBarWindowId);
        Assert.Equal(shellWindowId, bodyWindowId);
        Assert.StartsWith("avalonia/", shellWindowId, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"/{expectedKind.ToLowerInvariant()}-", shellWindowId, StringComparison.OrdinalIgnoreCase);

        var shellViewId = await GetRequiredAttributeAsync(shell, BlazorWindowPage.ViewIdAttribute);
        var titleBarViewId = await GetRequiredAttributeAsync(titleBar, BlazorWindowPage.ViewIdAttribute);
        var bodyViewId = await GetRequiredAttributeAsync(body, BlazorWindowPage.ViewIdAttribute);
        Assert.Equal($"{shellWindowId}/{BlazorWindowPage.WindowViewRole}", shellViewId);
        Assert.Equal($"{shellWindowId}/{BlazorWindowPage.TitleBarViewRole}", titleBarViewId);
        Assert.Equal($"{shellWindowId}/{BlazorWindowPage.WindowViewRole}", bodyViewId);

        Assert.Equal(shellViewId, await GetRequiredAttributeAsync(shell, BlazorWindowPage.AutomationIdAttribute));
        Assert.Equal(titleBarViewId, await GetRequiredAttributeAsync(titleBar, BlazorWindowPage.AutomationIdAttribute));
        Assert.Equal(bodyViewId, await GetRequiredAttributeAsync(body, BlazorWindowPage.AutomationIdAttribute));

        Assert.Equal(BlazorWindowPage.WindowViewRole, await GetRequiredAttributeAsync(shell, BlazorWindowPage.ViewRoleAttribute));
        Assert.Equal(BlazorWindowPage.TitleBarViewRole, await GetRequiredAttributeAsync(titleBar, BlazorWindowPage.ViewRoleAttribute));
        Assert.Equal(BlazorWindowPage.WindowViewRole, await GetRequiredAttributeAsync(body, BlazorWindowPage.ViewRoleAttribute));

        var shellDataContextType = await GetRequiredAttributeAsync(shell, BlazorWindowPage.DataContextTypeAttribute);
        var titleBarDataContextType = await GetRequiredAttributeAsync(titleBar, BlazorWindowPage.DataContextTypeAttribute);
        var bodyDataContextType = await GetRequiredAttributeAsync(body, BlazorWindowPage.DataContextTypeAttribute);
        Assert.Equal(shellDataContextType, titleBarDataContextType);
        Assert.Equal(shellDataContextType, bodyDataContextType);
        Assert.EndsWith(BlazorWindowPage.ExpectedDataContextTypeSuffix, shellDataContextType, StringComparison.Ordinal);
    }

    private static async Task WaitForCounterChangeAsync(IPage page, WindowDiagnostics diagnostics, string baselineCounter, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var currentCounter = await page.Locator(BlazorWindowPage.CounterSelector).InnerTextAsync();
            diagnostics.RecordCounterProbe(currentCounter);
            if (currentCounter != baselineCounter)
            {
                return;
            }

            await Task.Delay(150);
        }

        throw new TimeoutException(
            $"Window counter did not change within {timeout.TotalSeconds:N0}s after click. " +
            $"See diagnostic dump for page text, URL, console, and page errors.");
    }

    private static async Task<string> GetTagNameAsync(ILocator locator)
    {
        var tagName = await locator.EvaluateAsync<string>("element => element.tagName");
        Assert.False(string.IsNullOrWhiteSpace(tagName));
        return tagName;
    }

    private static async Task<string> GetRequiredAttributeAsync(ILocator locator, string attributeName)
    {
        var attributeValue = await locator.GetAttributeAsync(attributeName);
        Assert.False(string.IsNullOrWhiteSpace(attributeValue), $"Expected attribute '{attributeName}' to be present.");
        return attributeValue!;
    }

    private sealed class WindowDiagnostics
    {
        private readonly IPage page;
        private readonly ConcurrentQueue<string> consoleMessages = new();
        private readonly ConcurrentQueue<string> pageErrors = new();
        private readonly ConcurrentQueue<string> counterSnapshots = new();
        private readonly EventHandler<IConsoleMessage> consoleHandler;
        private readonly EventHandler<string> pageErrorHandler;
        private bool attached;

        public WindowDiagnostics(IPage page)
        {
            this.page = page;
            consoleHandler = (_, message) => consoleMessages.Enqueue($"{message.Type}: {message.Text}");
            pageErrorHandler = (_, errorText) => pageErrors.Enqueue(errorText);
        }

        public void Attach()
        {
            if (attached)
            {
                return;
            }

            page.Console += consoleHandler;
            page.PageError += pageErrorHandler;
            attached = true;
        }

        public void Detach()
        {
            if (!attached)
            {
                return;
            }

            page.Console -= consoleHandler;
            page.PageError -= pageErrorHandler;
            attached = false;
        }

        public void RecordSnapshot(string label)
        {
            counterSnapshots.Enqueue($"{label}: {GetSnapshot()}");
        }

        public void RecordCounterProbe(string counterText)
        {
            counterSnapshots.Enqueue($"probe: counter='{counterText}'");
        }

        public string BuildFailureMessage(string scenario, Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Window diagnostics for {scenario}:");
            builder.AppendLine($"PageUrl: {page.Url}");
            builder.AppendLine($"PageTitle: {SafeGetAsync(() => page.TitleAsync())}");
            builder.AppendLine($"CurrentCounter: {SafeGetAsync(() => page.Locator(BlazorWindowPage.CounterSelector).InnerTextAsync())}");
            builder.AppendLine($"DisplayName: {SafeGetAsync(() => page.Locator(BlazorWindowPage.DisplayNameSelector).InnerTextAsync())}");
            builder.AppendLine("Snapshots:");
            foreach (var snapshot in counterSnapshots)
            {
                builder.AppendLine($"  {snapshot}");
            }

            builder.AppendLine("Console:");
            foreach (var message in consoleMessages)
            {
                builder.AppendLine($"  {message}");
            }

            builder.AppendLine("PageErrors:");
            foreach (var error in pageErrors)
            {
                builder.AppendLine($"  {error}");
            }

            builder.AppendLine($"Exception: {exception.GetType().Name}: {exception.Message}");
            return builder.ToString();
        }

        private string GetSnapshot()
        {
            var title = SafeGetAsync(() => page.Locator(BlazorWindowPage.TitleSelector).InnerTextAsync());
            var counter = SafeGetAsync(() => page.Locator(BlazorWindowPage.CounterSelector).InnerTextAsync());
            var shellWindowId = SafeGetNullableAsync(() => page.Locator(BlazorWindowPage.ShellSelector).GetAttributeAsync(BlazorWindowPage.WindowIdAttribute));
            var shellViewId = SafeGetNullableAsync(() => page.Locator(BlazorWindowPage.ShellSelector).GetAttributeAsync(BlazorWindowPage.ViewIdAttribute));
            var titleBarViewId = SafeGetNullableAsync(() => page.Locator(BlazorWindowPage.TitleBarSelector).GetAttributeAsync(BlazorWindowPage.ViewIdAttribute));
            var bodyViewId = SafeGetNullableAsync(() => page.Locator(BlazorWindowPage.BodySelector).GetAttributeAsync(BlazorWindowPage.ViewIdAttribute));
            return $"url='{page.Url}', title='{title}', counter='{counter}', shellWindowId='{shellWindowId}', shellViewId='{shellViewId}', titleBarViewId='{titleBarViewId}', bodyViewId='{bodyViewId}'";
        }

        private static string SafeGetAsync(Func<Task<string>> action)
        {
            try
            {
                return action().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                return $"<error: {e.Message}>";
            }
        }

        private static string SafeGetNullableAsync(Func<Task<string?>> action)
        {
            try
            {
                return action().GetAwaiter().GetResult() ?? "<null>";
            }
            catch (Exception e)
            {
                return $"<error: {e.Message}>";
            }
        }
    }
}
