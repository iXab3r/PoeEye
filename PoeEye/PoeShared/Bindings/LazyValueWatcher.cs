using System;
using DynamicData.Annotations;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Bindings
{
    public class LazyValueWatcher<T> : DisposableReactiveObject
    {
        private static readonly Binder<LazyValueWatcher<T>> Binder = new();

        static LazyValueWatcher()
        {
            Binder.Bind(x => x.Value != null).To(x => x.IsValueLoaded);
        }

        public LazyValueWatcher(IObservable<T> valueSource)
        {
            valueSource.Subscribe(x =>
            {
                Value = x;
                Revision++;
            }).AddTo(Anchors);
            Binder.Attach(this).AddTo(Anchors);
        }

        public T Value { get; [UsedImplicitly] private set; }

        public bool IsValueLoaded { get; [UsedImplicitly] private set; }

        public long Revision { get; [UsedImplicitly] private set; }
    }
}