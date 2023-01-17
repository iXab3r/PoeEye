using System.ComponentModel;
using PropertyBinder;

namespace PoeShared.Bindings;

public sealed class ObservableValueWrapper : DisposableReactiveObject
{
    private static readonly Binder<ObservableValueWrapper> Binder = new();
    private readonly IValueWatcher targetWatcher;

    static ObservableValueWrapper()
    {
        Binder.Bind(x => x.Watcher).To(x => x.targetWatcher.Source);
        Binder.BindIf(x => x.Watcher != null && x.Watcher.CanSetValue(x.Value), x => x.Value).To(x => x.Watcher.Value);
    }

    public ObservableValueWrapper(IObservable<object> valueSource, IValueWatcher targetWatcher)
    {
        this.targetWatcher = targetWatcher;
        valueSource.Subscribe(x =>
        {
            if (Watcher == null || !Watcher.CanSetValue(x))
            {
                Watcher = x != null ? CreateWatcher(x.GetType()) : default;
            }
                
            Value = x;
        }).AddTo(Anchors);
            
        Binder.Attach(this).AddTo(Anchors);
    }

    public object Value { get; private set; }

    private IGenericValue Watcher { get; set; }

    private static IGenericValue CreateWatcher(Type valueWatcherType)
    {
        var watcherType = typeof(GenericValue<>).MakeGenericType(valueWatcherType);
        var result = (IGenericValue)Activator.CreateInstance(watcherType);
        return result;
    }

    private interface IGenericValue : INotifyPropertyChanged
    {
        object Value { get; set; }

        bool CanSetValue(object value);
    }

    private sealed class GenericValue<T> : DisposableReactiveObject, IGenericValue
    {
        public T Value { get; set; }

        object IGenericValue.Value
        {
            get => Value;
            set
            {
                if (value != default && value is not T)
                {
                    throw new ArgumentException($"Supplied value must be of type {typeof(T)}, got {value.GetType()}, value: {value}, current value: {Value}");
                }

                Value = value != default ? (T)value : default;
            }
        }

        public bool CanSetValue(object value)
        {
            return value == null || value.GetType() == typeof(T);
        }
        
        protected override void FormatToString(ToStringBuilder builder)
        {
            base.FormatToString(builder);
            builder.Append(Equals(default(T), Value) ? $"NULL of {typeof(T).Name}" : Value.ToString());
        }
    }
}