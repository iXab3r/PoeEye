using System.Runtime.Versioning;
using Avalonia;
using WinFormsApplication = System.Windows.Forms.Application;
using WinFormsHighDpiMode = System.Windows.Forms.HighDpiMode;

namespace PoeShared.UI.Avalonia;

internal static class Program
{
    [STAThread]
    [SupportedOSPlatform("windows")]
    public static void Main(string[] args)
    {
        WinFormsApplication.SetHighDpiMode(WinFormsHighDpiMode.PerMonitorV2);
        WinFormsApplication.EnableVisualStyles();
        WinFormsApplication.SetCompatibleTextRenderingDefault(false);

        var options = AvaloniaAppOptions.Parse(args);
        BuildAvaloniaApp(options).StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp(AvaloniaAppOptions options)
    {
        return AppBuilder
            .Configure(() => new App(options))
            .UsePlatformDetect()
            .LogToTrace();
    }
}
