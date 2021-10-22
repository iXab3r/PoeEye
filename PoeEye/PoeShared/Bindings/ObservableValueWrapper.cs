using System;
using System.ComponentModel;
using DynamicData.Annotations;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Bindings
{
    internal sealed class ObservableValueWrapper : DisposableReactiveObject
    {
        private readonly IValueWatcher target;
        private static readonly Binder<ObservableValueWrapper> Binder = new();

        static ObservableValueWrapper()
        {
            Binder.Bind(x => x.Value == null ? default : x.Value.GetType()).To(x => x.ValueType);
            Binder.Bind(x => x.ValueType == null ? default : CreateWatcher(x.ValueType)).To(x => x.Watcher);
            Binder.Bind(x => x.Watcher).To(x => x.target.Source);
            Binder.BindIf(x => x.Watcher is IGenericValue, x => x.Value).To(x => ((IGenericValue)x.Watcher).Value);
        }

        public ObservableValueWrapper(IObservable<object> valueSource, IValueWatcher target)
        {
            this.target = target;
            valueSource.Subscribe(x => Value = x).AddTo(Anchors);
            Binder.Attach(this).AddTo(Anchors);
        }
        
        public object Value { get; private set; }
        
        public Type ValueType { get; private set; }
        
        public object Watcher { get; private set; }

        private static object CreateWatcher(Type valueWatcherType)
        {
            var watcherType = typeof(GenericValue<>).MakeGenericType(valueWatcherType);
            var result = Activator.CreateInstance(watcherType);
            return result;
        }

        private interface IGenericValue : INotifyPropertyChanged
        {
            object Value { get; set; }
        }

        private sealed class GenericValue<T> : DisposableReactiveObject, IGenericValue
        {
            object IGenericValue.Value
            {
                get => Value;
                set => Value = (T)value;
            }
            
            public T Value { get; set; }
        }
        
        private sealed class ObservableValueWatcher<T> : DisposableReactiveObject
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
}