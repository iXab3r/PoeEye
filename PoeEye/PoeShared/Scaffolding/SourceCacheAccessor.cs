using DynamicData;
using PoeShared.Services;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public sealed class SourceCacheAccessor<TObject, TKey> : DisposableReactiveObject
{
    private static readonly Binder<SourceCacheAccessor<TObject, TKey>> Binder = new();
    private readonly SharedResourceLatch isUpdating = new();
    private TObject value;

    static SourceCacheAccessor()
    {
    }

    public SourceCacheAccessor(TKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        Key = key;

        this.WhenAnyValue(x => x.Cache)
            .Select(x => x != null ? (IObservableCache<TObject, TKey>) x : new IntermediateCache<TObject, TKey>())
            .Select(x =>
            {
                var exists = x.Lookup(key);
                var watch = x.Watch(key);
                return exists.HasValue ? watch : watch.Prepend(new Change<TObject, TKey>(ChangeReason.Remove, key, default, default));
            })
            .Switch()
            .Subscribe(x =>
            {
                using var latch = isUpdating.Rent();
                switch (x.Reason)
                {
                    case ChangeReason.Add:
                    case ChangeReason.Update:
                        Value = x.Current;
                        IsEnabled = true;
                        break;
                    case ChangeReason.Remove:
                        IsEnabled = false;
                        Value = default;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            })
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.IsEnabled)
            .Subscribe(isEnabled =>
            {
                var cache = Cache;
                if (cache == null)
                {
                    return;
                }

                if (!isEnabled && cache.Lookup(key).HasValue)
                {
                    cache.RemoveKey(key);
                }
            })
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.Value)
            .Where(x => !isUpdating.IsBusy)
            .Select(x => new {cache = Cache, value = x})
            .Where(x => x.cache != null && x.value != null && x.cache.KeySelector(x.value) != null)
            .Subscribe(x => x.cache.AddOrUpdate(x.value))
            .AddTo(Anchors);

        Binder.Attach(this).AddTo(Anchors);
    }

    public TKey Key { get; }

    public bool IsEnabled { get; set; }

    public TObject Value
    {
        get => value;
        set
        {
            var cache = Cache;
            if (value != null)
            {
                if (cache != null)
                {
                    var valueKey = cache.KeySelector(value);
                    if (EqualityComparer<TKey>.Default.Equals(default, valueKey))
                    {
                        IsEnabled = false;
                    }
                    else if (!EqualityComparer<TKey>.Default.Equals(valueKey, Key))
                    {
                        throw new ArgumentException($"Provided value must have key {Key}, got {valueKey}");
                    }
                }
            }

            this.value = value;
        }
    }

    public ISourceCache<TObject, TKey> Cache { get; set; }
}