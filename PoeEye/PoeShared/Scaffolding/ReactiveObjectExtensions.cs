﻿using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Common.Logging.Configuration;
using Guards;
using JetBrains.Annotations;
using ReactiveUI;
using ReactiveUI.Legacy;

namespace PoeShared.Scaffolding
{
    public static class ReactiveObjectExtensions
    {
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

            return source
                .WhenAnyValue(sourcePropertyExtractor)
                .Subscribe(x => instance.RaisePropertyChanged(instancePropertyName.Value));
        }
    }
}
