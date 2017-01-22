using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public interface IOverlayWindowController : IDisposableReactiveObject
    {
        void RegisterChild([NotNull] IOverlayViewModel viewModel);

        void Activate();

        void ActivateLastActiveWindow();

        bool IsVisible { get; }
    }
}