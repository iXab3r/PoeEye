using System.Globalization;

namespace PoeShared.UI.Avalonia;

public sealed class AvaloniaAppOptions
{
    public const string BlazorContentMode = "blazor";
    public const string WindowHarnessContentMode = "window-harness";
    public const string SampleCounter = "counter";
    public const string SampleCounterAlt = "counter-alt";
    public const string SampleSlow = "slow";
    public const string SampleBroken = "broken";
    public const string LaunchSecondaryBlazorWindow = "--launch-secondary-blazor-window";
    public const string LaunchSecondaryBlazorDialog = "--launch-secondary-blazor-dialog";
    public const string SampleViewArg = "--sample-view";

    public AvaloniaAppOptions(int webView2DebugPort, string contentMode = BlazorContentMode, string sampleView = SampleCounter, string? secondaryBlazorWindowLaunchMode = null)
    {
        WebView2DebugPort = webView2DebugPort;
        ContentMode = string.IsNullOrWhiteSpace(contentMode)
            ? BlazorContentMode
            : contentMode.Trim().ToLowerInvariant();
        SampleView = string.IsNullOrWhiteSpace(sampleView)
            ? SampleCounter
            : sampleView.Trim().ToLowerInvariant();
        SecondaryBlazorWindowLaunchMode = secondaryBlazorWindowLaunchMode;
    }

    public int WebView2DebugPort { get; }

    public string ContentMode { get; }

    public string SampleView { get; }

    public string? SecondaryBlazorWindowLaunchMode { get; }

    public static AvaloniaAppOptions Default { get; } = new(0, BlazorContentMode, SampleCounter);

    public static AvaloniaAppOptions Parse(string[] args)
    {
        var webView2DebugPort = 0;
        var contentMode = BlazorContentMode;
        var sampleView = SampleCounter;
        string? secondaryWindowLaunchMode = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--webview2-debug-port=", StringComparison.OrdinalIgnoreCase))
            {
                var value = arg["--webview2-debug-port=".Length..];
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                {
                    webView2DebugPort = port;
                }

                continue;
            }

            if (string.Equals(arg, "--content-mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                contentMode = args[++i];
                continue;
            }

            if (arg.StartsWith("--content-mode=", StringComparison.OrdinalIgnoreCase))
            {
                contentMode = arg["--content-mode=".Length..];
                continue;
            }

            if (string.Equals(arg, SampleViewArg, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                sampleView = args[++i];
                continue;
            }

            if (arg.StartsWith("--sample-view=", StringComparison.OrdinalIgnoreCase))
            {
                sampleView = arg["--sample-view=".Length..];
                continue;
            }

            if (string.Equals(arg, LaunchSecondaryBlazorWindow, StringComparison.OrdinalIgnoreCase))
            {
                secondaryWindowLaunchMode = "modeless";
                continue;
            }

            if (string.Equals(arg, LaunchSecondaryBlazorDialog, StringComparison.OrdinalIgnoreCase))
            {
                secondaryWindowLaunchMode = "dialog";
            }
        }

        return new AvaloniaAppOptions(webView2DebugPort, contentMode, sampleView, secondaryWindowLaunchMode);
    }
}
