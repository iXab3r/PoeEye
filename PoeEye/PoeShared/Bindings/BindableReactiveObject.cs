using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using DynamicData;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public abstract class BindableReactiveObject : DisposableReactiveObject, IBindableReactiveObject
    {
        private static long GlobalIdx = 0;
        
        private readonly SourceCache<IReactiveBinding, string> bindings = new(x => x.TargetPropertyPath);
        private readonly string bindableObjectId = $"O-{Interlocked.Increment(ref GlobalIdx)}";

        protected BindableReactiveObject()
        {
            Log = this.PrepareLogger(nameof(BindableReactiveObject)).WithSuffix(bindableObjectId).WithSuffix(ToString);
            bindings.Connect()
                .OnItemRemoved(x => x.Dispose())
                .Bind(out var bindingsList)
                .Subscribe()
                .AddTo(Anchors);
            BindingsList = bindingsList;
            Bindings = bindings;
        }
        
        private IFluentLog Log { get; }

        public IObservableCache<IReactiveBinding, string> Bindings { get; }

        public ReadOnlyObservableCollection<IReactiveBinding> BindingsList { get; }

        public void RemoveBinding(string targetPropertyName)
        {
            if (string.IsNullOrEmpty(targetPropertyName))
            {
                return;
            }
            var existingBindingsToRemove = bindings.Items.Where(x => x.TargetPropertyPath.StartsWith(targetPropertyName)).ToArray();
            if (existingBindingsToRemove.Any())
            {
                Log.Debug(() => $"Removing bindings(count: {existingBindingsToRemove.Length}) for {targetPropertyName}:\n\t{existingBindingsToRemove.ToStringTable()}");
                existingBindingsToRemove.ForEach(RemoveBinding);
            }
        }

        public void RemoveBinding(IReactiveBinding binding)
        {
            Log.Debug(() => $"Removing binding: {binding}");
            bindings.Remove(binding);
        }

        public void ClearBindings()
        {
            Log.Debug(() => $"Clearing bindings, count: {bindings.Count}");
            bindings.Clear();
        }

        public IReactiveBinding AddOrUpdateBinding<TSource>(string targetPropertyName, TSource source, string sourcePath) where TSource : DisposableReactiveObject
        {
            Log.Debug(() => $"Adding binding for '{targetPropertyName}', source path: {sourcePath}, source: {source}");
            
            var sourceWatcher = new PropertyPathWatcher() { Source = source, PropertyPath = sourcePath };
            var targetWatcher = new PropertyPathWatcher() { Source = this, PropertyPath = targetPropertyName };
            var newBinding = new ReactiveBinding(targetPropertyName, sourceWatcher, targetWatcher);
            return AddOrUpdateBinding(newBinding);
        }

        public IReactiveBinding AddOrUpdateBinding(IValueProvider valueSource, string targetPropertyName)
        {
            var targetWatcher = new PropertyPathWatcher
            {
                PropertyPath = targetPropertyName,
            };

            var binding = new ReactiveBinding(targetPropertyName, valueSource, targetWatcher);
            var anchor = AddOrUpdateBinding(binding);
            targetWatcher.Source = this;
            return anchor;
        }

        internal IReactiveBinding AddOrUpdateBinding(IReactiveBinding binding)
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
            return binding;
        }

        public IReactiveBinding ResolveBinding(string targetPropertyName)
        {
            var result = bindings.Lookup(targetPropertyName);
            return result.HasValue ? result.Value : default;
        }

        private IEnumerable<string> IteratePath(string propertyPath)
        {
            var propertyParts = propertyPath.Split('.').SkipLast(1);
            var combined = new StringBuilder(propertyPath.Length);

            foreach (var part in propertyParts)
            {
                combined.Append(part);
                yield return combined.ToString();
                combined.Append(".");
            }
        }
    }
}