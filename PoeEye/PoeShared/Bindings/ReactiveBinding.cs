using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PropertyBinder;
using PropertyChanged;

namespace PoeShared.Bindings
{
    internal class ReactiveBinding : DisposableReactiveObject, IReactiveBinding
    {
        private static readonly Binder<ReactiveBinding> Binder = new();

        static ReactiveBinding()
        {
            Binder
                .Bind(x => x.SourceWatcher.HasValue && x.TargetWatcher.HasValue)
                .To(x => x.IsActive);
            
            Binder.Bind(x => new[]
            {
                x.TargetWatcher.Error == null ? null : $"Target Watcher error: {x.TargetWatcher.Error}",
                x.SourceWatcher.Error == null ? null : $"Source Watcher error: {x.SourceWatcher.Error}"
            }.Where(y => y != null).JoinStrings(Environment.NewLine)).To((x, v) => x.Error = string.IsNullOrEmpty(v) ? default : v);
            
            Binder.BindIf(x => x.IsActive, x => x.SourceWatcher.Value)
                .ElseIf(x => x.TargetWatcher.HasValue, x => default)
                .To((x, v) =>
                {
                    x.TargetWatcher?.SetCurrentValue(v);
                });
        }
        
        public ReactiveBinding(string targetPropertyPath, IValueProvider sourceWatcher, IValueWatcher targetWatcher)
        {
            TargetPropertyPath = targetPropertyPath;
            SourceWatcher = sourceWatcher.AddTo(Anchors);
            TargetWatcher = targetWatcher.AddTo(Anchors);
            Binder.Attach(this).AddTo(Anchors);
        }

        public string TargetPropertyPath { get; }
        
        public string Error { get; private set; }

        public IValueProvider SourceWatcher { get; }
        
        public IValueWatcher TargetWatcher { get; }
        
        public bool IsActive { get; private set; }
    }
    
    internal sealed class ReactiveBinding<TSource, TTarget, TProperty> : ReactiveBinding, IReactiveBinding where TSource : class where TTarget : class
    {
        public ReactiveBinding(Expression<Func<TTarget, TProperty>> targetPropertyAccessor, Expression<Func<TSource, TProperty>> sourceAccessor) 
            : this(targetPropertyAccessor.GetMemberName(), targetPropertyAccessor, sourceAccessor)
        {
            
        }
        
        public ReactiveBinding(string targetPropertyPath, Expression<Func<TTarget, TProperty>> targetPropertyAccessor, Expression<Func<TSource, TProperty>> sourceAccessor) 
         : base(targetPropertyPath, new ExpressionWatcher<TSource, TProperty>(sourceAccessor), new ExpressionWatcher<TTarget, TProperty>(targetPropertyAccessor))
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

        IValueProvider IReactiveBinding.SourceWatcher => SourceWatcher;

        IValueWatcher IReactiveBinding.TargetWatcher => TargetWatcher;
    }
}