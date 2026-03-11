using System.Threading.Tasks;
using PoeShared.Blazor.Wpf;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.WinForms.Services;

internal sealed class BlazorHostController : IBlazorHostController
{
    private readonly IBlazorContentHost contentHost;

    public BlazorHostController(IBlazorContentHost contentHost)
    {
        this.contentHost = contentHost;
    }

    public ICommandWrapper ReloadCommand => contentHost.ReloadCommand;

    public ICommandWrapper OpenDevToolsCommand => contentHost.OpenDevToolsCommand;

    public ICommandWrapper ZoomInCommand => contentHost.ZoomInCommand;

    public ICommandWrapper ZoomOutCommand => contentHost.ZoomOutCommand;

    public ICommandWrapper ResetZoomCommand => contentHost.ResetZoomCommand;

    public bool Focus()
    {
        return contentHost.Focus();
    }

    public Task Reload()
    {
        return contentHost.Reload();
    }

    public Task OpenDevTools()
    {
        return contentHost.OpenDevTools();
    }

    public Task ZoomIn()
    {
        return contentHost.ZoomIn();
    }

    public Task ZoomOut()
    {
        return contentHost.ZoomOut();
    }

    public Task ResetZoom()
    {
        return contentHost.ResetZoom();
    }
}
