using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PropertyBinder;
using PropertyChanged;

namespace PoeShared.Bindings
{
    public class ReactiveBinding : DisposableReactiveObject, IReactiveBinding
    {
        private static readonly Binder<ReactiveBinding> Binder = new Binder<ReactiveBinding>();

        static ReactiveBinding()
        {
            Binder.BindIf(x => x.TargetWatcher.HasValue && x.SourceWatcher.HasValue, x => x.SourceWatcher.Value)
                .ElseIf(x => x.TargetWatcher.HasValue, x => default)
                .To((x, v) =>
                {
                    if (x.TargetWatcher != null)
                    {
                        x.TargetWatcher.SetCurrentValue(v);
                    }
                });
            
            Binder
                .Bind(x => x.SourceWatcher.HasValue && x.TargetWatcher.HasValue)
                .To(x => x.IsActive);
        }

        public ReactiveBinding(IValueWatcher sourceWatcher, IValueWatcher targetWatcher) : this(sourceWatcher, targetWatcher, targetWatcher.ToString())
        {
        }
        
        public ReactiveBinding(IValueWatcher sourceWatcher, IValueWatcher targetWatcher, string targetPropertyPath)
        {
            SourceWatcher = sourceWatcher.AddTo(Anchors);
            TargetWatcher = targetWatcher.AddTo(Anchors);
            TargetPropertyPath = targetPropertyPath;
            if (!TargetWatcher.CanSetValue)
            {
                throw new ArgumentException($"Invalid target property expression - can not set value: {sourceWatcher} => {targetWatcher}");
            }

            Binder.Attach(this).AddTo(Anchors);
        }

        public string TargetPropertyPath { get; }
        
        public IValueWatcher SourceWatcher { get; }
        
        public IValueWatcher TargetWatcher { get; }
        
        public bool IsActive { get; private set; }
    }
    
    public sealed class ReactiveBinding<TSource, TTarget, TProperty> : ReactiveBinding, IReactiveBinding where TSource : class where TTarget : class
    {
        public ReactiveBinding(Expression<Func<TSource, TProperty>> sourceAccessor, Expression<Func<TTarget, TProperty>> targetPropertyAccessor) 
         : base(new ExpressionWatcher<TSource, TProperty>(sourceAccessor), new ExpressionWatcher<TTarget, TProperty>(targetPropertyAccessor))
        {
            SourceWatcher = (ExpressionWatcher<TSource, TProperty>)base.SourceWatcher;
            TargetWatcher = (ExpressionWatcher<TTarget, TProperty>)base.TargetWatcher;
        }

        [DoNotNotify]
        public TSource Source
        {
            get => SourceWatcher.Source;
            set => SourceWatcher.Source = value;
        }

        [DoNotNotify]
        public TTarget Target
        {
            get => TargetWatcher.Source;
            set => TargetWatcher.Source = value;
        }

        public new ExpressionWatcher<TSource, TProperty> SourceWatcher { get; }

        public new ExpressionWatcher<TTarget, TProperty> TargetWatcher { get; }

        IValueWatcher IReactiveBinding.SourceWatcher => SourceWatcher;

        IValueWatcher IReactiveBinding.TargetWatcher => TargetWatcher;
    }
}