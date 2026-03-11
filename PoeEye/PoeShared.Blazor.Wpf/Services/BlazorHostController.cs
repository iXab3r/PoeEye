using System.Threading.Tasks;
using PoeShared.Blazor.Wpf;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Services;

internal sealed class BlazorHostController : IBlazorHostController
{
    private readonly IBlazorContentControl contentControl;

    public BlazorHostController(IBlazorContentControl contentControl)
    {
        this.contentControl = contentControl;
    }

    public ICommandWrapper ReloadCommand => contentControl.ReloadCommand;

    public ICommandWrapper OpenDevToolsCommand => contentControl.OpenDevToolsCommand;

    public ICommandWrapper ZoomInCommand => contentControl.ZoomInCommand;

    public ICommandWrapper ZoomOutCommand => contentControl.ZoomOutCommand;

    public ICommandWrapper ResetZoomCommand => contentControl.ResetZoomCommand;

    public bool Focus()
    {
        return contentControl.Focus();
    }

    public Task Reload()
    {
        return contentControl.Reload();
    }

    public Task OpenDevTools()
    {
        return contentControl.OpenDevTools();
    }

    public Task ZoomIn()
    {
        return contentControl.ZoomIn();
    }

    public Task ZoomOut()
    {
        return contentControl.ZoomOut();
    }

    public Task ResetZoom()
    {
        return contentControl.ResetZoom();
    }
}
