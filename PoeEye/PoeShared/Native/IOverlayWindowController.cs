using System;
using System.Collections.Generic;
using System.Windows;
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