using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

using JetBrains.Annotations;
using log4net;
using ReactiveUI;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace PoeShared.Scaffolding
{
    public static class ReactiveObjectExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReactiveObjectExtensions));

        public static IDisposable RaiseWhenSourceValue<TSource, TTarget, TSourceProperty, TTargetProperty>(
            [NotNull] this TTarget instance,
            [NotNull] Expression<Func<TTarget, TTargetProperty>> instancePropertyExtractor,
            [NotNull] TSource source,
            [NotNull] Expression<Func<TSource, TSourceProperty>> sourcePropertyExtractor,
            [CanBeNull] IScheduler scheduler = null)
            where TSource : INotifyPropertyChanged
            where TTarget : IDisposableReactiveObject
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(instancePropertyExtractor, nameof(instancePropertyExtractor));
            Guard.ArgumentNotNull(sourcePropertyExtractor, nameof(sourcePropertyExtractor));
            Guard.ArgumentNotNull(source, nameof(source));

            var instancePropertyName = new Lazy<string>(() => Reflection.ExpressionToPropertyNames(instancePropertyExtractor.Body));
            var sourcePropertyName = new Lazy<string>(() => Reflection.ExpressionToPropertyNames(sourcePropertyExtractor.Body));

            var result = source
                .WhenAnyValue(sourcePropertyExtractor);

            if (scheduler != null)
            {
                result = result.ObserveOn(scheduler);
            }
            
            return result
                .DistinctUntilChanged()
                .Subscribe(x =>
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"[{typeof(TSource).Name}.{sourcePropertyName.Value} => {typeof(TTarget).Name}.{instancePropertyName.Value}] Bound property '{sourcePropertyName.Value}' (source {source}) fired, raising {instancePropertyName.Value} on {instance}");
                    }
                    instance.RaisePropertyChanged(instancePropertyName.Value);
                }, Log.HandleException);
        }

        public static ObservableAsPropertyHelper<TSourceProperty> ToPropertyHelper<TSource, TSourceProperty>(
            [NotNull] this TSource instance,
            [NotNull] Expression<Func<TSource, TSourceProperty>> instancePropertyExtractor,
            [NotNull] IObservable<TSourceProperty> sourceObservable,
            [CanBeNull] IScheduler scheduler = null)
            where TSource : IDisposableReactiveObject
        {
            return ToProperty(instance, instancePropertyExtractor, sourceObservable, default, false, scheduler);
        }
        
        public static ObservableAsPropertyHelper<TSourceProperty> ToProperty<TSource, TSourceProperty>(
            [NotNull] this TSource instance,
            [NotNull] Expression<Func<TSource, TSourceProperty>> instancePropertyExtractor,
            [NotNull] IObservable<TSourceProperty> sourceObservable,
            [CanBeNull] TSourceProperty initialValue,
            bool deferSubscription,
            [CanBeNull] IScheduler scheduler)
            where TSource : IDisposableReactiveObject
        {
            var instancePropertyName = new Lazy<string>(() => Reflection.ExpressionToPropertyNames(instancePropertyExtractor.Body));

            var result = new ObservableAsPropertyHelper<TSourceProperty>(
                observable: sourceObservable.DistinctUntilChanged(),
                onChanged: x => instance.RaisePropertyChanged(instancePropertyName.Value),
                initialValue: initialValue,
                deferSubscription: deferSubscription,
                scheduler);
            return result;
        }

        [Obsolete("Use RaiseWhenSourceValue instead")]
        public static IDisposable BindPropertyTo<TSource, TTarget, TSourceProperty, TTargetProperty>(
            [NotNull] this TTarget instance,
            [NotNull] Expression<Func<TTarget, TTargetProperty>> instancePropertyExtractor,
            [NotNull] TSource source,
            [NotNull] Expression<Func<TSource, TSourceProperty>> sourcePropertyExtractor,
            [NotNull] IScheduler scheduler)
            where TSource : INotifyPropertyChanged
            where TTarget : IDisposableReactiveObject
        {
            return RaiseWhenSourceValue(instance, instancePropertyExtractor, source, sourcePropertyExtractor, scheduler);
        }
        
        [Obsolete("Use RaiseWhenSourceValue instead")]
        public static IDisposable BindPropertyTo<TSource, TTarget, TSourceProperty, TTargetProperty>(
            [NotNull] this TTarget instance,
            [NotNull] Expression<Func<TTarget, TTargetProperty>> instancePropertyExtractor,
            [NotNull] TSource source,
            [NotNull] Expression<Func<TSource, TSourceProperty>> sourcePropertyExtractor)
            where TSource : INotifyPropertyChanged
            where TTarget : IDisposableReactiveObject
        {
            return RaiseWhenSourceValue(instance, instancePropertyExtractor, source, sourcePropertyExtractor);
        }

        public static IDisposable LinkObjectProperties<TSource, TSourceProperty, TTargetProperty>(
            [NotNull] this TSource instance,
            [NotNull] Expression<Func<TSource, TSourceProperty>> instancePropertyExtractor,
            [NotNull] Expression<Func<TSource, TTargetProperty>> sourcePropertyExtractor)
            where TSource : IDisposableReactiveObject
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(instancePropertyExtractor, nameof(instancePropertyExtractor));
            Guard.ArgumentNotNull(sourcePropertyExtractor, nameof(sourcePropertyExtractor));

            var instancePropertyName = new Lazy<string>(() => Reflection.ExpressionToPropertyNames(instancePropertyExtractor.Body));

            return instance
                .WhenAnyValue(sourcePropertyExtractor)
                .Subscribe(x => instance.RaisePropertyChanged(instancePropertyName.Value), Log.HandleException);
        }
        
        public static void RaiseIfChanged<TSource, TSourceProperty>(
            [NotNull] this TSource instance, 
            [NotNull] string instancePropertyName,
            TSourceProperty previous, TSourceProperty current)
            where TSource : IDisposableReactiveObject
        {
            if (!EqualityComparer<TSourceProperty>.Default.Equals(previous, current))
            {
                instance.RaisePropertyChanged(instancePropertyName);
            }
        }
    }
}