using PoeShared.Blazor.Wpf;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Avalonia;

internal sealed class AvaloniaBlazorHostController : IBlazorHostController
{
    private readonly AvaloniaBlazorWindow window;

    public AvaloniaBlazorHostController(AvaloniaBlazorWindow window)
    {
        this.window = window;
        ReloadCommand = BlazorCommandWrapper.Create(Reload);
        OpenDevToolsCommand = BlazorCommandWrapper.Create(OpenDevTools);
        ZoomInCommand = BlazorCommandWrapper.Create(ZoomIn);
        ZoomOutCommand = BlazorCommandWrapper.Create(ZoomOut);
        ResetZoomCommand = BlazorCommandWrapper.Create(ResetZoom);
    }

    public ICommandWrapper ReloadCommand { get; }

    public ICommandWrapper OpenDevToolsCommand { get; }

    public ICommandWrapper ZoomInCommand { get; }

    public ICommandWrapper ZoomOutCommand { get; }

    public ICommandWrapper ResetZoomCommand { get; }

    public bool Focus()
    {
        window.Activate();
        return true;
    }

    public Task Reload()
    {
        window.ReloadContentHost();
        return Task.CompletedTask;
    }

    public Task OpenDevTools()
    {
        window.ShowDevTools();
        return Task.CompletedTask;
    }

    public Task ZoomIn()
        => window.ZoomInAsync();

    public Task ZoomOut()
        => window.ZoomOutAsync();

    public Task ResetZoom()
        => window.ResetZoomAsync();
}
