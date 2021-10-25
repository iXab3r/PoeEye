using System;
using System.Collections.ObjectModel;
using DynamicData;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public interface IBindableReactiveObject : IDisposableReactiveObject
    {
        IObservableCache<IReactiveBinding, string> Bindings { get; }
        ReadOnlyObservableCollection<IReactiveBinding> BindingsList { get; }
        void RemoveBinding(string targetPropertyName);
        void ClearBindings();
        IDisposable AddOrUpdateBinding<TSource>(string targetPropertyName, TSource source, string sourcePath) where TSource : DisposableReactiveObject;
        IDisposable AddOrUpdateBinding(IReactiveBinding binding);
        IReactiveBinding ResolveBinding(string targetPropertyName);
    }
}