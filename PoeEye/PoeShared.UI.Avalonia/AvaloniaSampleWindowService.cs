using System.Reactive.Linq;
using PoeShared.Blazor.Avalonia;
using PoeShared.Blazor.Wpf;

namespace PoeShared.UI.Avalonia;

public sealed class AvaloniaSampleWindowService : IDisposable
{
    private readonly AvaloniaBlazorWindow ownerWindow;
    private readonly int browserDebugPort;
    private readonly List<IBlazorWindow> openWindows = new();
    private int windowVersion = 1;

    public AvaloniaSampleWindowService(AvaloniaBlazorWindow ownerWindow, int browserDebugPort)
    {
        this.ownerWindow = ownerWindow;
        this.browserDebugPort = browserDebugPort;
    }

    public event EventHandler? WindowsChanged;

    public int ActiveWindowCount
    {
        get
        {
            lock (openWindows)
            {
                return openWindows.Count;
            }
        }
    }

    public void OpenWindow(AvaloniaSampleDefinition definition, MainCounterViewModel sharedContent, bool shareContent, bool isModal)
    {
        var kindLabel = isModal ? "Dialog" : "Modeless";
        var windowId = windowVersion++;
        var windowContent = shareContent
            ? sharedContent
            : new MainCounterViewModel($"{definition.DisplayName} window content ({kindLabel}) #{windowId}");
        var viewParameters = new Dictionary<string, object?>
        {
            [nameof(Blazor.WindowContentHostView.SampleViewType)] = definition.ViewType,
            [nameof(Blazor.WindowContentHostView.SampleAcceptsDataContext)] = definition.AcceptsDataContext,
            [nameof(Blazor.WindowContentHostView.WindowTitle)] = $"{definition.DisplayName} {kindLabel} Window #{windowId}",
            [nameof(Blazor.WindowContentHostView.KindLabel)] = kindLabel,
            [nameof(Blazor.WindowContentHostView.DataContext)] = windowContent
        };

        var window = new AvaloniaBlazorWindow(browserDebugPort, ownerWindow.NativeWindow)
        {
            AutomationId = $"avalonia/{definition.Key}/{kindLabel.ToLowerInvariant()}-{windowId}",
            ViewType = typeof(Blazor.WindowContentHostView),
            Title = $"{definition.DisplayName} {kindLabel} Window #{windowId}",
            Width = 960,
            Height = 640,
            MinWidth = 480,
            MinHeight = 320,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            ShowInTaskbar = true,
            Topmost = false,
            ViewParameters = viewParameters
        };
        viewParameters[nameof(Blazor.WindowContentHostView.RequestClose)] = (Action)(() => window.Close());

        lock (openWindows)
        {
            openWindows.Add(window);
        }

        window
            .WhenClosed
            .Take(1)
            .Subscribe(_ =>
            {
                lock (openWindows)
                {
                    openWindows.Remove(window);
                }

                if (!shareContent)
                {
                    windowContent.Dispose();
                }

                window.Dispose();
                WindowsChanged?.Invoke(this, EventArgs.Empty);
            });

        WindowsChanged?.Invoke(this, EventArgs.Empty);

        if (isModal)
        {
            window.ShowDialog();
            return;
        }

        window.Show();
    }

    public void CloseAllWindows()
    {
        IBlazorWindow[] windows;
        lock (openWindows)
        {
            windows = openWindows.ToArray();
        }

        foreach (var window in windows)
        {
            try
            {
                window.Close();
            }
            catch
            {
                window.Dispose();
            }
        }
    }

    public void ShowDevTools()
    {
        ownerWindow.ShowDevTools();
    }

    public void GrowMainWindow(int deltaWidth = 160, int deltaHeight = 120)
    {
        var rect = ownerWindow.GetWindowRect();
        ownerWindow.SetWindowSize(new System.Drawing.Size(rect.Width + deltaWidth, rect.Height + deltaHeight));
    }

    public string GetOwnerWindowHandleHex()
    {
        return $"0x{ownerWindow.GetWindowHandle().ToInt64():X}";
    }

    public string GetBoundsApplyStatus()
    {
        return ownerWindow.LastBoundsApplyStatus;
    }

    public void BeginResize(WindowResizeDirection direction, int startScreenX, int startScreenY)
    {
        ownerWindow.BeginResize(direction, new System.Drawing.Point(startScreenX, startScreenY));
    }

    public void Dispose()
    {
        CloseAllWindows();
        WindowsChanged = null;
    }
}
