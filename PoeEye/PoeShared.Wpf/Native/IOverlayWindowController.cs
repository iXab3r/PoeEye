using System;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

public interface IOverlayWindowController : IDisposableReactiveObject, IHasVisible
{
    bool ShowWireframes { get; set; }
        
    IDisposable RegisterChild(IOverlayViewModel viewModel);

    IObservableList<IOverlayViewModel> Children { get; }
}