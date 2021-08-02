using System;
using System.Reactive.Disposables;
using PoeShared.Native;

namespace PoeShared.Scaffolding
{
    public static class OverlayViewModelExtensions
    {
        public static IDisposable Show(this IOverlayViewModel overlay)
        {
            overlay.IsVisible = true;
            return Disposable.Create(() => overlay.IsVisible = false);
        }
    }
}