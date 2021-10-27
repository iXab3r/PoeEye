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
        private static long BindableObjectId = 0;
        
        private readonly SourceCache<IReactiveBinding, string> bindings = new(x => x.Key);
        private long bindableObjectId = Interlocked.Increment(ref BindableObjectId);

        protected BindableReactiveObject()
        {
            Log = this.PrepareLogger(nameof(BindableReactiveObject)).WithSuffix("Id " + bindableObjectId);
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
            Log.Debug(() => $"Trying to remove binding '{targetPropertyName}'");
            var existingBindingsToRemove = bindings.Items.Where(x => x.Key.StartsWith(targetPropertyName)).ToArray();
            if (existingBindingsToRemove.Any())
            {
                Log.Debug(() => $"Cleaning up existing bindings(count: {existingBindingsToRemove.Length}) for {targetPropertyName}:\n\t{existingBindingsToRemove.ToStringTable()}");
                existingBindingsToRemove.ForEach(RemoveBinding);
            }
            else
            {
                Log.Debug(() => $"No matching bindings for '{targetPropertyName}', bindings: {(bindings.Count > 0? $"\n\t{bindings.Items.ToStringTable()}" : "NO")}");
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

        public IDisposable AddOrUpdateBinding<TSource>(string targetPropertyName, TSource source, string sourcePath) where TSource : DisposableReactiveObject
        {
            Log.Debug(() => $"Adding binding for '{targetPropertyName}', source path: {sourcePath}, source: {source}");
            
            var sourceWatcher = new PropertyPathWatcher() { Source = source, PropertyPath = sourcePath };
            var targetWatcher = new PropertyPathWatcher() { Source = this, PropertyPath = targetPropertyName };
            var newBinding = new ReactiveBinding(sourceWatcher, targetWatcher);
            return AddOrUpdateBinding(newBinding);
        }

        public IDisposable AddOrUpdateBinding(IReactiveBinding binding)
        {
            Log.Debug(() => $"Adding binding with key {binding.Key}: {binding}");
            
            foreach (var propertyPathPart in IteratePath(binding.Key))
            {
                if (bindings.Lookup(propertyPathPart).HasValue)
                {
                    RemoveBinding(propertyPathPart);
                    break;
                }
            }
            
            RemoveBinding(binding.Key);
            bindings.AddOrUpdate(binding);
            return Disposable.Create(() =>
            {
                Log.Debug(() => $"Binding creating anchor was disposed - cleaning up binding {binding}");
                RemoveBinding(binding);
            });
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