using System.Text;
using DynamicData;
using JetBrains.Annotations;

namespace PoeShared.Bindings;

public abstract class BindableReactiveObject : DisposableReactiveObject, IBindableReactiveObject
{
    private static long globalIdx;

    private readonly SourceCache<IReactiveBinding, string> bindings = new(x => x.TargetPropertyPath);

    protected BindableReactiveObject()
    {
        ObjectId = $"O#{Interlocked.Increment(ref globalIdx)}";
        Log = GetType().PrepareLogger()
            .WithSuffix(ToString)
            .WithSuffix(ObjectId);
        bindings.Connect()
            .OnItemRemoved(x => x.Dispose())
            .Subscribe()
            .AddTo(Anchors);
        Bindings = bindings;

        bindings
            .CountChanged
            .Select(x => x > 0)
            .DistinctUntilChanged()
            .Subscribe(x => HasBindings = x)
            .AddTo(Anchors);
    }

    protected IFluentLog Log { get; }
    
    /// <inheritdoc />
    public string ObjectId { get; }

    /// <inheritdoc />
    public bool HasBindings { get; [UsedImplicitly] private set; }

    /// <inheritdoc />
    public IObservableCache<IReactiveBinding, string> Bindings { get; }

    /// <inheritdoc />
    public void RemoveBinding(string targetPropertyPath)
    {
        if (string.IsNullOrEmpty(targetPropertyPath))
        {
            return;
        }
        var existingBindingsToRemove = bindings.Items.Where(x => x.TargetPropertyPath.StartsWith(targetPropertyPath)).ToArray();
        if (existingBindingsToRemove.Any())
        {
            Log.Debug(() => $"Removing bindings(count: {existingBindingsToRemove.Length}) for {targetPropertyPath}:\n\t{existingBindingsToRemove.DumpToTable()}");
            existingBindingsToRemove.ForEach(RemoveBinding);
        }
    }

    /// <inheritdoc />
    public void ClearBindings()
    {
        Log.Debug(() => $"Clearing bindings, count: {bindings.Count}");
        bindings.Clear();
    }

    /// <inheritdoc />
    public IReactiveBinding AddOrUpdateBinding<TSource>(string targetPropertyPath, TSource source, string sourcePropertyPath) where TSource : DisposableReactiveObject
    {
        Log.Debug(() => $"Adding binding for '{targetPropertyPath}', source path: {sourcePropertyPath}, source: {source}");
            
        var sourceWatcher = new PropertyPathWatcher() { Source = source, PropertyPath = sourcePropertyPath };
        var targetWatcher = new PropertyPathWatcher() { Source = this, PropertyPath = targetPropertyPath };
        var newBinding = new ReactiveBinding(targetPropertyPath, sourceWatcher, targetWatcher);
        AddOrUpdateBinding(newBinding);
        return newBinding;
    }

    /// <inheritdoc />
    public IReactiveBinding AddOrUpdateBinding(IValueProvider valueSource, string targetPropertyPath)
    {
        var targetWatcher = new PropertyPathWatcher
        {
            PropertyPath = targetPropertyPath,
        };

        var binding = new ReactiveBinding(targetPropertyPath, valueSource, targetWatcher);
        AddOrUpdateBinding(binding);
        targetWatcher.Source = this;
        return binding;
    }

    /// <inheritdoc />
    public IReactiveBinding ResolveBinding(string propertyPath)
    {
        var result = bindings.Lookup(propertyPath);
        return result.HasValue ? result.Value : default;
    }

    /// <inheritdoc />
    public void RemoveBinding(IReactiveBinding binding)
    {
        Log.Debug(() => $"Removing binding: {binding}");
        bindings.Remove(binding);
    }

    /// <inheritdoc />
    public void AddOrUpdateBinding(IReactiveBinding binding)
    {
        Log.Debug(() => $"Adding binding with key {binding.TargetPropertyPath}: {binding}");
            
        foreach (var propertyPathPart in IteratePath(binding.TargetPropertyPath))
        {
            if (bindings.Lookup(propertyPathPart).HasValue)
            {
                RemoveBinding(propertyPathPart);
                break;
            }
        }
        RemoveBinding(binding.TargetPropertyPath);

        bindings.AddOrUpdate(binding);
    }

    private static IEnumerable<string> IteratePath(string propertyPath)
    {
        var propertyParts = propertyPath.Split('.').SkipLast(1);
        var combined = new StringBuilder(propertyPath.Length);

        foreach (var part in propertyParts)
        {
            combined.Append(part);
            yield return combined.ToString();
            combined.Append('.');
        }
    }
}