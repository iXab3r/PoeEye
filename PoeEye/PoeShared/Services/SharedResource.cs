using System;
using System.Reactive.Disposables;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Services
{
    public sealed class SharedResource<T> : DisposableReactiveObject where T : SharedResourceBase
    {
        private static readonly IFluentLog Log = typeof(SharedResource<T>).PrepareLogger();

        private readonly Func<T> factory;

        public SharedResource(Func<T> factory)
        {
            this.factory = factory;
            Disposable.Create(() => Instance = default).AddTo(Anchors);
            this.WhenAnyValue(x => x.Instance)
                .DisposePrevious()
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
        }

        private T Instance { get; set; }

        public T RentOrCreate()
        {
            if (Instance == default || !Instance.TryRent() || Instance.Anchors.IsDisposed)
            {
                Log.Debug(() => $"{(Instance == default ? $"Initializing new instance of type {typeof(T)}" : $"Re-initializing instance of type {typeof(T)}")}");
                var newInstance = factory();

                if (!newInstance.TryRent())
                {
                    throw new InvalidOperationException($"Failed to rent newly-created instance of type {typeof(T)}: {newInstance}, refCount: {newInstance.RefCount}");
                }
                Instance = newInstance;
                Log.Debug(() => $"Created new instance of type {typeof(T)}: {Instance}");
            }

            return Instance;
        }
    }
}