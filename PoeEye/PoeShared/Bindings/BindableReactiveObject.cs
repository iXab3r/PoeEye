using System;
using System.Linq;
using System.Reactive.Disposables;
using DynamicData;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public abstract class BindableReactiveObject : DisposableReactiveObject, IBindableReactiveObject
    {
        private readonly SourceCache<IReactiveBinding, string> bindings = new SourceCache<IReactiveBinding, string>(x => x.TargetPropertyPath);

        protected BindableReactiveObject()
        {
            bindings.Connect()
                .OnItemRemoved(x => x.Dispose())
                .Subscribe()
                .AddTo(Anchors);
            Bindings = bindings;
        }

        public IObservableCache<IReactiveBinding, string> Bindings { get; }

        public void RemoveBinding(string targetPropertyName)
        {
            var binding = ResolveBinding(targetPropertyName);
            if (binding == null)
            {
                throw new InvalidOperationException($"Failed to resolve binding for {targetPropertyName}");
            }
            bindings.Remove(binding);
        }

        public void ClearBindings()
        {
            bindings.Clear();
        }

        public IDisposable AddOrUpdateBinding<TSource>(string targetPropertyName, TSource source, string sourcePath) where TSource : DisposableReactiveObject
        {
            var binding = ResolveBinding(targetPropertyName);
            if (binding != null)
            {
                bindings.Remove(binding);
            }

            var sourceWatcher = new PropertyPathWatcher() { Source = source, PropertyPath = sourcePath };
            var targetWatcher = new PropertyPathWatcher() { Source = this, PropertyPath = targetPropertyName };
            var newBinding = new ReactiveBinding(sourceWatcher, targetWatcher);
            return AddOrUpdateBinding(newBinding);
        }

        public IDisposable AddOrUpdateBinding(IReactiveBinding binding)
        {
            bindings.AddOrUpdateIfNeeded(binding);
            return Disposable.Create(() => bindings.Remove(binding));
        }

        private IReactiveBinding ResolveBinding(string targetPropertyName)
        {
            var result = bindings.Lookup(targetPropertyName);
            return result.HasValue ? result.Value : default;
        }
    }
}