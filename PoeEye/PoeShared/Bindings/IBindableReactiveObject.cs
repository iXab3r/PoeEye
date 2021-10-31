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
        IReactiveBinding AddOrUpdateBinding<TSource>(string targetPropertyName, TSource source, string sourcePath) where TSource : DisposableReactiveObject;
        IReactiveBinding AddOrUpdateBinding(IValueProvider valueSource, string targetPropertyName);
        IReactiveBinding ResolveBinding(string targetPropertyName);
    }
}