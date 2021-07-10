using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Native
{
    public interface IOverlayWindowController : IDisposableReactiveObject
    {
        bool IsVisible { get; }
        
        bool IsEnabled { get; set; }
        
        IDisposable RegisterChild([NotNull] IOverlayViewModel viewModel);

        void ActivateLastActiveWindow();

        [NotNull]
        IOverlayViewModel[] GetChildren();
    }
}