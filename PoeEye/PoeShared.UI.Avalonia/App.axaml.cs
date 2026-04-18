using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Avalonia;
using PropertyChanged;
using Application = Avalonia.Application;
using AvaloniaWindow = global::Avalonia.Controls.Window;

namespace PoeShared.UI.Avalonia;

[DoNotNotify]
public partial class App : Application
{
    private readonly AvaloniaAppOptions options;
    private AvaloniaBlazorWindow? mainBlazorWindow;
    private AvaloniaSampleWindowService? sampleWindowService;

    public App()
        : this(AvaloniaAppOptions.Default)
    {
    }

    public App(AvaloniaAppOptions options)
    {
        this.options = options;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private AvaloniaWindow CreateMainWindow()
    {
        var debugPort = options.WebView2DebugPort != 0
            ? options.WebView2DebugPort
            : GetFreePort();

        mainBlazorWindow = new AvaloniaBlazorWindow(debugPort)
        {
            AutomationId = "avalonia/main",
            Width = 960,
            Height = 640,
            MinWidth = 640,
            MinHeight = 400,
            ShowInTaskbar = true,
            Title = "PoeShared Blazor Avalonia",
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
        };
        sampleWindowService = new AvaloniaSampleWindowService(mainBlazorWindow, debugPort);
        mainBlazorWindow.ViewType = typeof(Blazor.SampleBrowserView);
        mainBlazorWindow.ConfigureServices = services => services.AddSingleton(sampleWindowService);
        mainBlazorWindow.ViewParameters = new Dictionary<string, object?>
        {
            [nameof(Blazor.SampleBrowserView.InitialSampleViewKey)] = options.SampleView,
            [nameof(Blazor.SampleBrowserView.AutoLaunchWindowMode)] = options.SecondaryBlazorWindowLaunchMode
        };

        mainBlazorWindow.Closed += OnBlazorMainWindowClosed;
        return mainBlazorWindow.NativeWindow;
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private void OnBlazorMainWindowClosed(object? sender, EventArgs e)
    {
        sampleWindowService?.Dispose();
        mainBlazorWindow?.Dispose();
        sampleWindowService = null;
        mainBlazorWindow = null;
    }
}
