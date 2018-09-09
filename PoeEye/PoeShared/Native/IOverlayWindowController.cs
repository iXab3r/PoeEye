using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public interface IOverlayWindowController : IDisposableReactiveObject
    {
        IDisposable RegisterChild([NotNull] IOverlayViewModel viewModel);

        void ActivateLastActiveWindow();

        bool IsVisible { get; }

        [NotNull]
        IOverlayViewModel[] GetChilds();
    }
}