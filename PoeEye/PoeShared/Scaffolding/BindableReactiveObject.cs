using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using DynamicData;
using PoeShared.Logging;
using Binder = PropertyBinder.Binder;

namespace PoeShared.Scaffolding
{
    public abstract class BindableReactiveObject : DisposableReactiveObject
    {
        private readonly SourceList<BinderConfig> bindings = new SourceList<BinderConfig>();

        protected BindableReactiveObject()
        {
            bindings.Connect().DisposeMany()
                .Bind(out var bindingsSrc)
                .Subscribe().AddTo(Anchors);
            Bindings = bindingsSrc;
        }

        public ReadOnlyObservableCollection<BinderConfig> Bindings { get; }

        public void RemoveBinding(string targetPropertyName)
        {
            var binding = ResolveBinding(targetPropertyName);
            if (binding == null)
            {
                throw new InvalidOperationException($"Failed to resolve binding for {targetPropertyName}");
            }
            bindings.Remove(binding);
        }

        public IDisposable AddOrUpdateBinding<TSource>(string targetPropertyName, TSource source, string sourcePath) where TSource : DisposableReactiveObject
        {
            var binding = ResolveBinding(targetPropertyName);
            if (binding != null)
            {
                bindings.Remove(binding);
            }
            var binderData = new BinderConfig(this, source, sourcePath, this, targetPropertyName);
            bindings.Add(binderData);
            return Disposable.Create(() => bindings.Remove(binderData));
        }

        private BinderConfig ResolveBinding(string targetPropertyName)
        {
            return bindings.Items.FirstOrDefault(x => string.Equals(targetPropertyName, x.TargetPropertyName));
        }
    }
}