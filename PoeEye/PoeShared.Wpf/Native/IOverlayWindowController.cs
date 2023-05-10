using System;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public interface IOverlayWindowController : IDisposableReactiveObject
{
    bool IsVisible { get; }
        
    bool ShowWireframes { get; set; }
        
    IDisposable RegisterChild(IOverlayViewModel viewModel);

    IObservableList<IOverlayViewModel> Children { get; }
}