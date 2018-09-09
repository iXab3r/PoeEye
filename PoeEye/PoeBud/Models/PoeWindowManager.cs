using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity.Attributes;

namespace PoeBud.Models
{
    internal sealed class PoeWindowManager : DisposableReactiveObject, IPoeWindowManager
    {
        private readonly ConcurrentDictionary<IntPtr, IPoeWindow> poeWindowByHandle = new ConcurrentDictionary<IntPtr, IPoeWindow>();
        private readonly IWindowTracker poeWindowTracker;
        private readonly IFactory<IPoeWindow, IntPtr> windowsFactory;

        private IPoeWindow activeWindow;

        public PoeWindowManager(
            [NotNull] IFactory<IPoeWindow, IntPtr> windowsFactory,
            [NotNull] [Dependency(WellKnownWindows.PathOfExileWindow)]
            IWindowTracker poeWindowTracker)
        {
            Guard.ArgumentNotNull(windowsFactory, nameof(windowsFactory));

            this.windowsFactory = windowsFactory;
            this.poeWindowTracker = poeWindowTracker;

            poeWindowTracker
                .WhenAnyValue(x => x.MatchingWindowHandle)
                .DistinctUntilChanged()
                .Subscribe(WindowActivated)
                .AddTo(Anchors);
        }

        public IPoeWindow ActiveWindow
        {
            get => activeWindow;
            set => this.RaiseAndSetIfChanged(ref activeWindow, value);
        }

        private void WindowActivated(IntPtr activeWindowHandle)
        {
            Log.Instance.Debug($"[PoeWindowManager] Active window: '{poeWindowTracker.ActiveWindowTitle}'");
            ActiveWindow = activeWindowHandle == IntPtr.Zero
                ? null
                : poeWindowByHandle.GetOrAdd(activeWindowHandle, x => windowsFactory.Create(x));
        }
    }
}