using System;
using System.Reactive.Disposables;
using System.Threading;
using log4net;
using PoeShared.Logging;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Services
{
    internal sealed class SharedResourceLatch : DisposableReactiveObject, ISharedResourceLatch
    {
        private static readonly IFluentLog Log = typeof(SharedResourceLatch).PrepareLogger();

        private long counter = 0;
        private string name;

        public bool IsBusy => counter > 0;

        public string Name
        {
            get => name;
            set => RaiseAndSetIfChanged(ref name, value);
        }

        public IDisposable Rent()
        {
            var wasPaused = IsBusy;
            var counterAfterIncrement = Interlocked.Increment(ref counter);
            if (!wasPaused)
            {
                Log.Debug($"[{this}] Marked as busy: {counterAfterIncrement}");
                RaisePropertyChanged(nameof(IsBusy));
            }
            else
            {
                Log.Debug($"[{this}] Already in use: {counterAfterIncrement}");
            }

            return Disposable.Create(() =>
            {
                var counterAfterDecrement = Interlocked.Decrement(ref counter);
                if (!IsBusy)
                {
                    Log.Debug($"[{this}] Released: {counterAfterDecrement}");
                    RaisePropertyChanged(nameof(IsBusy));
                }
                else
                {
                    Log.Debug($"[{this}] Still in use: {counterAfterDecrement}");
                }
            });
        }

        public override string ToString()
        {
            return $"{Name}Latch({counter})";
        }
    }
}