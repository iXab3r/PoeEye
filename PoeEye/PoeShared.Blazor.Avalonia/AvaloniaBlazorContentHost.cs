using System.IO;
using System.Runtime.Versioning;
using System.Diagnostics;
using Microsoft.AspNetCore.Components.WebView;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using PropertyChanged;
using PoeShared.Blazor.Wpf;
using WinFormsDockStyle = System.Windows.Forms.DockStyle;
using WinFormsPanel = System.Windows.Forms.Panel;

namespace PoeShared.Blazor.Avalonia;

[SupportedOSPlatform("windows")]
[DoNotNotify]
public sealed class AvaloniaBlazorContentHost : NativeControlHost, IDisposable
{
    private readonly int browserDebugPort;
    private readonly Type rootComponentType;
    private readonly IReadOnlyDictionary<string, object?> rootParameters;
    private readonly WinFormsPanel hostPanel = new();
    private readonly BlazorWebView blazorWebView;
    private readonly ServiceProvider serviceProvider;
    private Microsoft.Web.WebView2.WinForms.WebView2? currentWebView;
    private bool disposed;

    public AvaloniaBlazorContentHost(
        int browserDebugPort,
        Type rootComponentType,
        IReadOnlyDictionary<string, object?>? rootParameters,
        Action<IServiceCollection>? configureServices = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("AvaloniaBlazorContentHost currently supports Windows only.");
        }

        this.browserDebugPort = browserDebugPort;
        this.rootComponentType = rootComponentType ?? throw new ArgumentNullException(nameof(rootComponentType));
        this.rootParameters = rootParameters ?? new Dictionary<string, object?>();
        blazorWebView = new BlazorWebView();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Information));
        services.AddWindowsFormsBlazorWebView();
        services.AddBlazorWebViewDeveloperTools();
        configureServices?.Invoke(services);
        serviceProvider = services.BuildServiceProvider();

        blazorWebView.Dock = WinFormsDockStyle.Fill;
        blazorWebView.HostPage = ResolveHostPagePath();
        blazorWebView.Services = serviceProvider;
        blazorWebView.BlazorWebViewInitializing += OnBlazorWebViewInitializing;
        blazorWebView.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
        blazorWebView.RootComponents.Add(new RootComponent(
            "#app",
            this.rootComponentType,
            new Dictionary<string, object?>(this.rootParameters)));

        hostPanel.Dock = WinFormsDockStyle.Fill;
        hostPanel.Controls.Add(blazorWebView);
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        hostPanel.CreateControl();
        blazorWebView.CreateControl();
        return new PlatformHandle(hostPanel.Handle, "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        Dispose();
        base.DestroyNativeControlCore(control);
    }

    protected override global::Avalonia.Size MeasureOverride(global::Avalonia.Size availableSize)
    {
        var desiredWidth = double.IsInfinity(availableSize.Width)
            ? Math.Max(1, hostPanel.Width)
            : availableSize.Width;
        var desiredHeight = double.IsInfinity(availableSize.Height)
            ? Math.Max(1, hostPanel.Height)
            : availableSize.Height;

        return new global::Avalonia.Size(desiredWidth, desiredHeight);
    }

    protected override global::Avalonia.Size ArrangeOverride(global::Avalonia.Size finalSize)
    {
        ResizeHostedControls(finalSize);
        return finalSize;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        blazorWebView.BlazorWebViewInitializing -= OnBlazorWebViewInitializing;
        blazorWebView.BlazorWebViewInitialized -= OnBlazorWebViewInitialized;
        blazorWebView.Dispose();
        hostPanel.Dispose();
        serviceProvider.Dispose();
    }

    public void ShowDevTools()
    {
        _ = ShowDevToolsAsync();
    }

    public Task ReloadAsync()
    {
        return InvokeOnWebViewThreadAsync(async webView =>
        {
            await webView.EnsureCoreWebView2Async();
            webView.Reload();
        });
    }

    public Task ZoomInAsync()
    {
        return InvokeOnWebViewThreadAsync(async webView =>
        {
            await webView.EnsureCoreWebView2Async();
            webView.ZoomFactor += 0.1;
        });
    }

    public Task ZoomOutAsync()
    {
        return InvokeOnWebViewThreadAsync(async webView =>
        {
            await webView.EnsureCoreWebView2Async();
            webView.ZoomFactor -= 0.1;
        });
    }

    public Task ResetZoomAsync()
    {
        return InvokeOnWebViewThreadAsync(async webView =>
        {
            await webView.EnsureCoreWebView2Async();
            webView.ZoomFactor = 1;
        });
    }

    private async Task ShowDevToolsAsync()
    {
        try
        {
            if (blazorWebView.IsDisposed)
            {
                return;
            }

            await InvokeOnWebViewThreadAsync(ShowDevToolsCoreAsync);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to open WebView2 DevTools: {ex}");
        }
    }

    private static string ResolveHostPagePath()
        => Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");

    private void OnBlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
        e.EnvironmentOptions ??= new CoreWebView2EnvironmentOptions();
        e.UserDataFolder = ResolveUserDataFolder(browserDebugPort);
        if (browserDebugPort <= 0)
        {
            return;
        }

        e.EnvironmentOptions.AdditionalBrowserArguments = AppendBrowserArgument(
            e.EnvironmentOptions.AdditionalBrowserArguments,
            $"--remote-debugging-port={browserDebugPort}");
    }

    private static string ResolveUserDataFolder(int browserDebugPort)
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "PoeShared.Blazor.Avalonia",
            "BlazorWebView",
            browserDebugPort > 0 ? browserDebugPort.ToString() : "default");

        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string AppendBrowserArgument(string? existingArguments, string newArgument)
    {
        if (string.IsNullOrWhiteSpace(existingArguments))
        {
            return newArgument;
        }

        return existingArguments.Contains(newArgument, StringComparison.OrdinalIgnoreCase)
            ? existingArguments
            : $"{existingArguments} {newArgument}";
    }

    private void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        currentWebView = e.WebView;
    }

    private async Task ShowDevToolsCoreAsync(Microsoft.Web.WebView2.WinForms.WebView2 webView)
    {
        await webView.EnsureCoreWebView2Async();
        webView.CoreWebView2.OpenDevToolsWindow();
    }

    private async Task InvokeOnWebViewThreadAsync(Func<Microsoft.Web.WebView2.WinForms.WebView2, Task> operation)
    {
        if (blazorWebView.IsDisposed)
        {
            return;
        }

        if (blazorWebView.InvokeRequired)
        {
            var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            blazorWebView.BeginInvoke(new Action(async () =>
            {
                try
                {
                    var nestedWebView = currentWebView ?? blazorWebView.WebView;
                    if (nestedWebView != null)
                    {
                        await operation(nestedWebView);
                    }

                    completionSource.SetResult();
                }
                catch (Exception ex)
                {
                    completionSource.SetException(ex);
                }
            }));

            await completionSource.Task;
            return;
        }

        var webView = currentWebView ?? blazorWebView.WebView;
        if (webView == null)
        {
            return;
        }

        await operation(webView);
    }

    private void ResizeHostedControls(global::Avalonia.Size finalSize)
    {
        var width = Math.Max(1, (int)Math.Ceiling(finalSize.Width));
        var height = Math.Max(1, (int)Math.Ceiling(finalSize.Height));

        if (hostPanel.Width == width && hostPanel.Height == height)
        {
            return;
        }

        hostPanel.SuspendLayout();
        try
        {
            hostPanel.Width = width;
            hostPanel.Height = height;
            blazorWebView.Width = width;
            blazorWebView.Height = height;
        }
        finally
        {
            hostPanel.ResumeLayout(performLayout: true);
        }
    }
}
