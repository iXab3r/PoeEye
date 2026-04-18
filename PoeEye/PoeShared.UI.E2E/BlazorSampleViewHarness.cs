using System.Collections.Concurrent;
using System.Text;
using Microsoft.Playwright;
using Xunit;

namespace PoeShared.UI.E2E;

internal static class BlazorSampleViewHarness
{
    public static Task<IPage> WaitForSampleBrowserPageAsync(IBrowser browser, TimeSpan timeout)
        => BrowserPageDiscovery.WaitForPageWithSelectorAsync(browser, "[data-testid='sample-browser-root']", timeout);

    public static async Task SelectSampleViewAsync(IPage page, AvaloniaSampleView sampleView)
    {
        await page.Locator("[data-testid='view-type-select']").WaitForAsync();
        await page.SelectOptionAsync("[data-testid='view-type-select']", sampleView.ToCommandLineKey());
        await page.Locator(GetRootSelector(sampleView)).WaitForAsync();
    }

    public static Task AssertCounterAltAsync(IPage page)
        => AssertSampleViewAsync(
            page,
            "counter-alt sample",
            async diagnostics =>
            {
                var heading = page.Locator("[data-testid='counter-alt-title']");
                await heading.WaitForAsync();
                Assert.Equal("Counter Alt", await heading.InnerTextAsync());

                await page.Locator("[data-testid='counter-alt-display-name']").WaitForAsync();
                await page.Locator("[data-testid='counter-alt-instance-id']").WaitForAsync();
                var countLocator = page.Locator("[data-testid='counter-alt-value']");
                await countLocator.WaitForAsync();
                Assert.Equal("0", await countLocator.InnerTextAsync());

                await page.Locator("[data-testid='counter-alt-increment-five-button']").ClickAsync();

                await WaitForTextAsync(countLocator, "5", TimeSpan.FromSeconds(10), diagnostics);
            });

    public static Task AssertSlowAsync(IPage page)
        => AssertSampleViewAsync(
            page,
            "slow sample",
            async diagnostics =>
            {
                var heading = page.Locator("[data-testid='slow-title']");
                await heading.WaitForAsync();
                Assert.Equal("Slow View", await heading.InnerTextAsync());

                var statusLocator = page.Locator("[data-testid='slow-status']");
                await WaitForTextAsync(statusLocator, "exiting", TimeSpan.FromSeconds(15), diagnostics);
            });

    public static Task AssertBrokenAsync(IPage page)
        => AssertSampleViewAsync(
            page,
            "broken sample",
            async diagnostics =>
            {
                var heading = page.Locator("[data-testid='broken-title']");
                await heading.WaitForAsync();
                Assert.Equal("Broken View", await heading.InnerTextAsync());

                await page.Locator("[data-testid='broken-inside-throw-sync']").ClickAsync();

                var recoveryText = page.Locator("[data-testid='broken-error-content']");
                await recoveryText.WaitForAsync();

                var recoverButton = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Try to recover" });
                await recoverButton.WaitForAsync();
                await recoverButton.ClickAsync();

                await page.Locator("[data-testid='broken-inside-throw-sync']").WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 10_000
                });
            });

    private static async Task AssertSampleViewAsync(IPage page, string scenario, Func<SampleDiagnostics, Task> assertAction)
    {
        var diagnostics = new SampleDiagnostics(page);
        diagnostics.Attach();
        try
        {
            await page.Locator("body").WaitForAsync();
            await assertAction(diagnostics);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(diagnostics.BuildFailureMessage(scenario, ex), ex);
        }
        finally
        {
            diagnostics.Detach();
        }
    }

    private static async Task WaitForTextAsync(ILocator locator, string expectedText, TimeSpan timeout, SampleDiagnostics diagnostics)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var currentText = await locator.InnerTextAsync();
            diagnostics.RecordTextProbe(currentText);
            if (currentText.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await Task.Delay(150);
        }

        throw new TimeoutException($"Timed out waiting for '{expectedText}'.");
    }

    private static string GetRootSelector(AvaloniaSampleView sampleView)
    {
        return sampleView switch
        {
            AvaloniaSampleView.Counter => "[data-testid='blazor-root']",
            AvaloniaSampleView.CounterAlt => "[data-testid='counter-alt-view']",
            AvaloniaSampleView.Slow => "[data-testid='slow-view']",
            AvaloniaSampleView.Broken => "[data-testid='broken-view']",
            _ => throw new ArgumentOutOfRangeException(nameof(sampleView), sampleView, null)
        };
    }

    private sealed class SampleDiagnostics
    {
        private readonly IPage page;
        private readonly ConcurrentQueue<string> consoleMessages = new();
        private readonly ConcurrentQueue<string> pageErrors = new();
        private readonly ConcurrentQueue<string> textSnapshots = new();
        private readonly EventHandler<IConsoleMessage> consoleHandler;
        private readonly EventHandler<string> pageErrorHandler;
        private bool attached;

        public SampleDiagnostics(IPage page)
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

        public void RecordTextProbe(string text)
        {
            textSnapshots.Enqueue($"probe: {text}");
        }

        public string BuildFailureMessage(string scenario, Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Sample-view diagnostics for {scenario}:");
            builder.AppendLine($"PageUrl: {page.Url}");
            builder.AppendLine($"PageTitle: {SafeGetAsync(() => page.TitleAsync())}");
            builder.AppendLine($"BodyText: {SafeGetAsync(() => page.Locator("body").InnerTextAsync())}");
            builder.AppendLine("Snapshots:");
            foreach (var snapshot in textSnapshots)
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
