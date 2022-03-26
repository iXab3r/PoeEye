using System.Collections.Immutable;

namespace PoeShared.Services;

public class ResourceChooser<T> : IEnumerable<T>
{
    private readonly IComparer<ItemContainer> containerComparer;
    private readonly IEqualityComparer<T> itemComparer;
    private readonly NamedLock gate = new(nameof(ResourceChooser<T>));
    private ImmutableList<ItemContainer> items;

    public ResourceChooser(IEnumerable<T> enumerable)
    {
        itemComparer = EqualityComparer<T>.Default;
        var itemsBuilder = ImmutableList.CreateBuilder<ItemContainer>();
        enumerable.Select(x => new ItemContainer(x, true)).ForEach(itemsBuilder.Add);
        items = itemsBuilder.ToImmutable();
        containerComparer = new LambdaComparer<ItemContainer>(
            (x, y) => x.Priority == y.Priority
                ? 0
                : x.Priority > y.Priority
                    ? 1
                    : -1);
    }
    
    public ResourceChooser() : this(ArraySegment<T>.Empty)
    {
    }

    public void Add(T resource)
    {
        using var @lock = gate.Enter();

        var item = new ItemContainer(
            resource,
            isAlive: true); 
        for (var i = 0; i < items.Count; i++)
        {
            if (containerComparer.Compare(item, items[i]) > 0)
            {
                items = items.Insert(i, item);
                return;
            }
        }
        items = items.Add(item);
    }

    public void Remove(T resource)
    {
        using var @lock = gate.Enter();

        for (var i = 0; i < items.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(items[i].Value, resource))
            {
                items = items.RemoveAt(i);
            }
        }
    }

    public void Clear()
    {
        using var @lock = gate.Enter();

        items = items.Clear();
    }
    
    public bool TryGetAlive(out T result)
    {
        using var @lock = gate.Enter();

        if (items.Count == 0)
        {
            result = default;
            return false;
        }
        var container = items[0];
        if (container.IsAlive)
        {
            result = container.Value;
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }

    public T GetAlive()
    {
        using var @lock = gate.Enter();

        if (items.Count == 0)
        {
            throw new InvalidOperationException($"There are no resources to choose from");
        }

        if (!TryGetAlive(out var result))
        {
            // all items are exhausted, returning the first one
            var container = items[0];
            return container.Value;
        }
        else
        {
            return result;
        }
    }

    public void Report(T resource, bool isAlive)
    {
        using var @lock = gate.Enter();
        
        for (var i = 0; i < items.Count; i++)
        {
            var container = items[i];
            if (itemComparer.Equals(container.Value, resource))
            {
                var newItem = new ItemContainer(container.Value, isAlive);
                var itemsBuilder = items.ToBuilder();
                itemsBuilder.RemoveAt(i);
                if (isAlive)
                {
                    var idx = items.FindIndex(x => x.IsAlive) + 1;
                    if (idx >= items.Count)
                    {
                        itemsBuilder.Add(newItem);
                    }
                    else
                    {
                        itemsBuilder.Insert(idx, newItem);
                    }
                }
                else
                {
                    itemsBuilder.Add(newItem);
                }
                items = itemsBuilder.ToImmutable();
                return;
            }
        }
    }
    
    public void ReportBroken(T resource)
    {
        Report(resource, isAlive: false);
    }
    
    public void ReportAlive(T resource)
    {
        Report(resource, isAlive: true);
    }
    
    private readonly struct ItemContainer
    {
        private const int IsAliveBasePriority = int.MaxValue / 2;

        public ItemContainer(T value, bool isAlive)
        {
            Value = value;
            
            IsAlive = isAlive;
            Priority = IsAlive ? IsAliveBasePriority : 0;
        }

        public T Value { get; }

        public bool IsAlive { get; }
        
        public int Priority { get; }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new MappingEnumerator<ItemContainer, T>(items.GetEnumerator(), x => x.Value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}