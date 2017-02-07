using System;
using System.Windows;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public interface IOverlayWindowController : IOverlayViewModel
    {
        void RegisterChild([NotNull] IOverlayViewModel viewModel);

        void Activate();

        void ActivateLastActiveWindow();

        bool IsVisible { get; }
    }
}