using Microsoft.Playwright;

namespace PoeShared.UI.E2E;

internal static class BrowserPageDiscovery
{
    public static async Task<IPage> WaitForPageWithSelectorAsync(IBrowser browser, string selector, TimeSpan timeout)
        => await WaitForPageWithAnySelectorAsync(browser, new[] { selector }, timeout);

    public static async Task<IPage> WaitForPageWithAnySelectorAsync(IBrowser browser, IReadOnlyCollection<string> selectors, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        Exception? lastError = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            foreach (var context in browser.Contexts)
            foreach (var page in context.Pages)
            {
                foreach (var selector in selectors.Where(selector => !string.IsNullOrWhiteSpace(selector)))
                {
                    try
                    {
                        await page.Locator(selector).WaitForAsync(new LocatorWaitForOptions
                        {
                            State = WaitForSelectorState.Visible,
                            Timeout = 500
                        });
                        return page;
                    }
                    catch (Exception e)
                    {
                        lastError = e;
                    }
                }
            }

            await Task.Delay(100);
        }

        var pageSummaries = new List<string>();
        foreach (var context in browser.Contexts)
        foreach (var page in context.Pages)
        {
            string bodyText;
            var consoleEntries = new List<string>();
            var pageErrors = new List<string>();
            try
            {
                bodyText = await page.Locator("body").InnerTextAsync();
            }
            catch (Exception e)
            {
                bodyText = $"<body unavailable: {e.Message}>";
            }

            try
            {
                await page.EvaluateAsync(
                    "() => JSON.stringify({ href: location.href, readyState: document.readyState, body: document.body?.innerText ?? '' })");
            }
            catch
            {
                // ignored
            }

            page.Console += (_, message) => consoleEntries.Add($"{message.Type}: {message.Text}");
            page.PageError += (_, errorText) => pageErrors.Add(errorText);

            pageSummaries.Add(
                $"url='{page.Url}', body='{bodyText}', console='{string.Join(" | ", consoleEntries)}', pageErrors='{string.Join(" | ", pageErrors)}'");
        }

        throw new TimeoutException(
            $"No page exposed any of selectors [{string.Join(", ", selectors)}]. " +
            $"LastError={lastError?.Message ?? "<none>"}. " +
            $"Pages={string.Join(" || ", pageSummaries)}");
    }
}
