using System.Collections.ObjectModel;
using DynamicData;
using ReactiveUI;

namespace PoeShared.Bindings;

public interface IBindableReactiveObject : IDisposableReactiveObject
{
    IObservableCache<IReactiveBinding, string> Bindings { get; }
    ReadOnlyObservableCollection<IReactiveBinding> BindingsList { get; }
    void RemoveBinding(string targetPropertyPath);
    void RemoveBinding(IReactiveBinding binding);
    void AddOrUpdateBinding(IReactiveBinding binding);
    void ClearBindings();
    IReactiveBinding AddOrUpdateBinding<TSource>(string targetPropertyPath, TSource source, string sourcePropertyPath) where TSource : DisposableReactiveObject;
    IReactiveBinding AddOrUpdateBinding(IValueProvider valueSource, string targetPropertyPath);
    IReactiveBinding ResolveBinding(string propertyPath);
}