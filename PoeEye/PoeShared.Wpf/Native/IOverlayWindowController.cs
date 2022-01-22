using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public interface IOverlayWindowController : IDisposableReactiveObject
{
    bool IsVisible { get; }
        
    bool IsEnabled { get; set; }
        
    bool ShowWireframes { get; set; }
        
    IDisposable RegisterChild([NotNull] IOverlayViewModel viewModel);

    void ActivateLastActiveWindow();

    [NotNull]
    IOverlayViewModel[] GetChildren();
}