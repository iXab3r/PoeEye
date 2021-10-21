using System;
using DynamicData.Annotations;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Bindings
{
    internal sealed class ObservableValueWatcher<T> : DisposableReactiveObject
    {
        private static readonly Binder<ObservableValueWatcher<T>> Binder = new();

        static ObservableValueWatcher()
        {
        }

        public ObservableValueWatcher(IObservable<T> valueSource)
        {
            valueSource.Subscribe(x =>
            {
                Value = x;
                Revision++;
            }).AddTo(Anchors);
            Binder.Attach(this).AddTo(Anchors);
        }

        public T Value { get; [UsedImplicitly] private set; }

        public long Revision { get; [UsedImplicitly] private set; }

        public override string ToString()
        {
            return $"ObservableValue, value: {Value}, revision: {Revision}";
        }
    }
}