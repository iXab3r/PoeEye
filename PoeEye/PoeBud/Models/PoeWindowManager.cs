using System.Collections.Concurrent;
using Microsoft.Practices.Unity;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeBud.Models
{
    using System;
    using System.Reactive.Linq;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using ReactiveUI;

    internal sealed class PoeWindowManager : DisposableReactiveObject, IPoeWindowManager
    {
        private readonly IFactory<IPoeWindow, IntPtr> windowsFactory;
        private readonly IWindowTracker poeWindowTracker;

        private readonly ConcurrentDictionary<IntPtr, IPoeWindow> poeWindowByHandle = new ConcurrentDictionary<IntPtr, IPoeWindow>();

        public PoeWindowManager(
            [NotNull] IFactory<IPoeWindow, IntPtr> windowsFactory,
            [NotNull] [Dependency(WellKnownWindows.PathOfExileWindow)] IWindowTracker poeWindowTracker)
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

        private void WindowActivated(IntPtr activeWindowHandle)
        {
            Log.Instance.Debug($"[PoeWindowManager] Active window: '{poeWindowTracker.ActiveWindowTitle}'");
            ActiveWindow = activeWindowHandle == IntPtr.Zero 
                ? null 
                : poeWindowByHandle.GetOrAdd(activeWindowHandle, (x) => windowsFactory.Create(x));
        }

        private IPoeWindow activeWindow;

        public IPoeWindow ActiveWindow
        {
            get { return activeWindow; }
            set { this.RaiseAndSetIfChanged(ref activeWindow, value); }
        }
    }
}
