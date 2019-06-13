using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Guards;
using JetBrains.Annotations;
using log4net;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

namespace PoeShared.Scaffolding
{
    public static class ReactiveObjectExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReactiveObjectExtensions));

        public static IDisposable BindPropertyTo<TSource, TTarget, TSourceProperty, TTargetProperty>(
            [NotNull] this TTarget instance,
            [NotNull] Expression<Func<TTarget, TTargetProperty>> instancePropertyExtractor,
            [NotNull] TSource source,
            [NotNull] Expression<Func<TSource, TSourceProperty>> sourcePropertyExtractor)
            where TSource : INotifyPropertyChanged
            where TTarget : IReactiveObject
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(instancePropertyExtractor, nameof(instancePropertyExtractor));
            Guard.ArgumentNotNull(sourcePropertyExtractor, nameof(sourcePropertyExtractor));
            Guard.ArgumentNotNull(source, nameof(source));

            var instancePropertyName = new Lazy<string>(() => Reflection.ExpressionToPropertyNames(instancePropertyExtractor.Body));
            var sourcePropertyName = new Lazy<string>(() => Reflection.ExpressionToPropertyNames(sourcePropertyExtractor.Body));

            return source
                .WhenAnyValue(sourcePropertyExtractor)
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

        public static IDisposable LinkObjectProperties<TSource, TSourceProperty, TTargetProperty>(
            [NotNull] this TSource instance,
            [NotNull] Expression<Func<TSource, TTargetProperty>> instancePropertyExtractor,
            [NotNull] Expression<Func<TSource, TSourceProperty>> sourcePropertyExtractor)
            where TSource : IReactiveObject
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(instancePropertyExtractor, nameof(instancePropertyExtractor));
            Guard.ArgumentNotNull(sourcePropertyExtractor, nameof(sourcePropertyExtractor));

            var instancePropertyName = new Lazy<string>(() => Reflection.ExpressionToPropertyNames(instancePropertyExtractor.Body));

            return instance
                .WhenAnyValue(sourcePropertyExtractor)
                .Subscribe(x => instance.RaisePropertyChanged(instancePropertyName.Value), Log.HandleException);
        }
    }
}