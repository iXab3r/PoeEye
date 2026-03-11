using System.Threading.Tasks;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

public interface IBlazorHostController
{
    ICommandWrapper ReloadCommand { get; }

    ICommandWrapper OpenDevToolsCommand { get; }

    ICommandWrapper ZoomInCommand { get; }

    ICommandWrapper ZoomOutCommand { get; }

    ICommandWrapper ResetZoomCommand { get; }

    bool Focus();

    Task Reload();

    Task OpenDevTools();

    Task ZoomIn();

    Task ZoomOut();

    Task ResetZoom();
}
