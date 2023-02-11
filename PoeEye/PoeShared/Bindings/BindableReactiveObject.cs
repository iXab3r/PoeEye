using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using DynamicData;

namespace PoeShared.Bindings;

public abstract class BindableReactiveObject : DisposableReactiveObject, IBindableReactiveObject
{
    private static long GlobalIdx;

    private readonly SourceCache<IReactiveBinding, string> bindings = new(x => x.TargetPropertyPath);

    protected BindableReactiveObject()
    {
        ObjectId = $"O#{Interlocked.Increment(ref GlobalIdx)}";
        Log = GetType().PrepareLogger()
            .WithSuffix(ToString)
            .WithSuffix(ObjectId);
        bindings.Connect()
            .OnItemRemoved(x => x.Dispose())
            .Bind(out var bindingsList)
            .Subscribe()
            .AddTo(Anchors);
        BindingsList = bindingsList;
        Bindings = bindings;
    }

    protected IFluentLog Log { get; }
    
    protected string ObjectId { get; }

    public IObservableCache<IReactiveBinding, string> Bindings { get; }

    public ReadOnlyObservableCollection<IReactiveBinding> BindingsList { get; }

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

    public void ClearBindings()
    {
        Log.Debug(() => $"Clearing bindings, count: {bindings.Count}");
        bindings.Clear();
    }

    public IReactiveBinding AddOrUpdateBinding<TSource>(string targetPropertyPath, TSource source, string sourcePropertyPath) where TSource : DisposableReactiveObject
    {
        Log.Debug(() => $"Adding binding for '{targetPropertyPath}', source path: {sourcePropertyPath}, source: {source}");
            
        var sourceWatcher = new PropertyPathWatcher() { Source = source, PropertyPath = sourcePropertyPath };
        var targetWatcher = new PropertyPathWatcher() { Source = this, PropertyPath = targetPropertyPath };
        var newBinding = new ReactiveBinding(targetPropertyPath, sourceWatcher, targetWatcher);
        AddOrUpdateBinding(newBinding);
        return newBinding;
    }

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

    public IReactiveBinding ResolveBinding(string propertyPath)
    {
        var result = bindings.Lookup(propertyPath);
        return result.HasValue ? result.Value : default;
    }

    public void RemoveBinding(IReactiveBinding binding)
    {
        Log.Debug(() => $"Removing binding: {binding}");
        bindings.Remove(binding);
    }

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