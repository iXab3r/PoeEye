using System.Collections.Concurrent;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace PoeShared.UI.E2E;

internal static class BlazorCounterHarness
{
    public static async Task<IPage> WaitForPageAsync(IBrowser browser, TimeSpan timeout)
        => await BrowserPageDiscovery.WaitForPageWithSelectorAsync(browser, BlazorCounterPage.RootSelector, timeout);

    public static async Task AssertBasicCounterBehaviorAsync(IPage page)
    {
        var diagnostics = new CounterDiagnostics(page);
        diagnostics.Attach();
        try
        {
            await AssertCounterFlowAsync(page, diagnostics, includeParitySignals: false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(diagnostics.BuildFailureMessage("basic counter flow", ex), ex);
        }
        finally
        {
            diagnostics.Detach();
        }
    }

    public static async Task AssertParityCounterBehaviorAsync(IPage page)
    {
        var diagnostics = new CounterDiagnostics(page);
        diagnostics.Attach();
        try
        {
            await AssertCounterFlowAsync(page, diagnostics, includeParitySignals: true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(diagnostics.BuildFailureMessage("parity counter flow", ex), ex);
        }
        finally
        {
            diagnostics.Detach();
        }
    }

    private static async Task AssertCounterFlowAsync(IPage page, CounterDiagnostics diagnostics, bool includeParitySignals)
    {
        await page.Locator(BlazorCounterPage.RootSelector).WaitForAsync();
        await page.Locator(BlazorCounterPage.TitleSelector).WaitForAsync();
        await page.Locator(BlazorCounterPage.CounterSelector).WaitForAsync();
        await page.Locator(BlazorCounterPage.IncrementButtonSelector).WaitForAsync();
        if (includeParitySignals)
        {
            await page.Locator(BlazorCounterPage.DisplayNameSelector).WaitForAsync();
            await page.Locator(BlazorCounterPage.InstanceIdSelector).WaitForAsync();
            await page.Locator(BlazorCounterPage.ReactiveCounterSelector).WaitForAsync();
        }

        var title = await page.Locator(BlazorCounterPage.TitleSelector).InnerTextAsync();
        Assert.Equal(BlazorCounterPage.ExpectedTitle, title);

        var counter = await page.Locator(BlazorCounterPage.CounterSelector).InnerTextAsync();
        Assert.Equal(BlazorCounterPage.ExpectedInitialCounter, counter);

        diagnostics.RecordSnapshot("before-click");
        await page.Locator(BlazorCounterPage.IncrementButtonSelector).ClickAsync();
        diagnostics.RecordSnapshot("after-click");

        await WaitForCounterChangeAsync(page, diagnostics, counter, TimeSpan.FromSeconds(5));

        var updatedCounter = await page.Locator(BlazorCounterPage.CounterSelector).InnerTextAsync();
        Assert.Equal("1", updatedCounter);
        if (includeParitySignals)
        {
            var displayName = await page.Locator(BlazorCounterPage.DisplayNameSelector).InnerTextAsync();
            Assert.Contains("Counter content", displayName, StringComparison.OrdinalIgnoreCase);

            var instanceId = await page.Locator(BlazorCounterPage.InstanceIdSelector).InnerTextAsync();
            Assert.False(string.IsNullOrWhiteSpace(instanceId));

            var reactiveCounter = await page.Locator(BlazorCounterPage.ReactiveCounterSelector).InnerTextAsync();
            Assert.Contains("1", reactiveCounter);
        }
    }

    private static async Task WaitForCounterChangeAsync(IPage page, CounterDiagnostics diagnostics, string baselineCounter, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var currentCounter = await page.Locator(BlazorCounterPage.CounterSelector).InnerTextAsync();
            diagnostics.RecordCounterProbe(currentCounter);
            if (currentCounter != baselineCounter)
            {
                return;
            }

            await Task.Delay(150);
        }

        throw new TimeoutException(
            $"Counter did not change within {timeout.TotalSeconds:N0}s after click. " +
            $"See diagnostic dump for page text, URL, console, and page errors.");
    }

    private sealed class CounterDiagnostics
    {
        private readonly IPage page;
        private readonly ConcurrentQueue<string> consoleMessages = new();
        private readonly ConcurrentQueue<string> pageErrors = new();
        private readonly ConcurrentQueue<string> counterSnapshots = new();
        private readonly EventHandler<IConsoleMessage> consoleHandler;
        private readonly EventHandler<string> pageErrorHandler;
        private bool attached;

        public CounterDiagnostics(IPage page)
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
            builder.AppendLine($"Phase2 diagnostics for {scenario}:");
            builder.AppendLine($"PageUrl: {page.Url}");
            builder.AppendLine($"PageTitle: {SafeGetAsync(() => page.TitleAsync())}");
            builder.AppendLine($"CurrentCounter: {SafeGetAsync(() => page.Locator(BlazorCounterPage.CounterSelector).InnerTextAsync())}");
            if (!string.IsNullOrWhiteSpace(BlazorCounterPage.DisplayNameSelector))
            {
                builder.AppendLine($"DisplayName: {SafeGetAsync(() => page.Locator(BlazorCounterPage.DisplayNameSelector).InnerTextAsync())}");
            }
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
            var title = SafeGetAsync(() => page.Locator(BlazorCounterPage.TitleSelector).InnerTextAsync());
            var counter = SafeGetAsync(() => page.Locator(BlazorCounterPage.CounterSelector).InnerTextAsync());
            return $"url='{page.Url}', title='{title}', counter='{counter}'";
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
    }
}
