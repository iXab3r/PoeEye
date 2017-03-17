using System;
using System.Windows;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public interface IOverlayWindowController : IDisposableReactiveObject
    {
        void RegisterChild([NotNull] IOverlayViewModel viewModel);

        void ActivateLastActiveWindow();

        bool IsVisible { get; }
    }
}