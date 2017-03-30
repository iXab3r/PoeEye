using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Common.Logging.Configuration;
using Guards;
using JetBrains.Annotations;
using ReactiveUI;
using ReactiveUI.Legacy;

namespace PoeShared.Scaffolding
{
    public static class ReactiveObjectExtensions
    {
        public static IDisposable BindPropertyTo<TSource, TTarget, TProperty>(
            [NotNull] this TTarget instance,
            [NotNull] string propertyName,
            [NotNull] TSource source,
            [NotNull] Expression<Func<TSource, TProperty>> sourcePropertyExtractor)
              where TSource : INotifyPropertyChanged
              where TTarget : IReactiveObject
        {
            Guard.ArgumentNotNull(() => instance);
            Guard.ArgumentNotNull(() => propertyName);
            Guard.ArgumentNotNull(() => sourcePropertyExtractor);
            Guard.ArgumentNotNull(() => source);

            return source
                .WhenAnyValue(sourcePropertyExtractor)
                .Subscribe(x => instance.RaisePropertyChanged(propertyName));
        }
    }
}