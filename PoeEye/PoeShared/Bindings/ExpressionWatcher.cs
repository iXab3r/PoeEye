using System;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reflection;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;
using Binder = PropertyBinder.Binder;

namespace PoeShared.Bindings
{
    public sealed class ExpressionWatcher : ExpressionWatcherBase
    {
        private static readonly Binder<ExpressionWatcher> Binder = new();

        static ExpressionWatcher()
        {
            Binder.Bind(x => x.SourceExpression).To(x => x.SourceExpression);
            Binder.Bind(x => x.ConditionExpression).To(x => x.ConditionExpression);
        }

        public ExpressionWatcher(Type propertyType)
        {
            PropertyType = propertyType;
            Binder.Attach(this).AddTo(Anchors);
        }
        
        public new string SourceExpression
        {
            get => base.SourceExpression;
            set => base.SourceExpression = value;
        }
        
        public new string ConditionExpression
        {
            get => base.ConditionExpression;
            set => base.ConditionExpression = value;
        }
    }

    public sealed class ExpressionWatcher<TSource, TProperty> : DisposableReactiveObject, IValueWatcher where TSource : class
    {
        private readonly Expression<Func<TSource, TProperty>> sourceAccessor;
        private readonly Expression<Func<TSource, bool>> condition;
        private readonly Binder<TSource> sourceBinder;
        private readonly SerialDisposable sourceBinderAnchors;
        private readonly Lazy<Action<TSource, TProperty>> assignmentActionSupplier;

        public ExpressionWatcher(Expression<Func<TSource, TProperty>> sourceAccessor, Expression<Func<TSource, bool>> condition)
        {
            Log = typeof(ExpressionWatcher<TSource, TProperty>).PrepareLogger().WithSuffix(ToString);

            this.sourceAccessor = sourceAccessor;
            this.condition = condition;
            sourceBinderAnchors = new SerialDisposable().AddTo(Anchors);
            SourceExpression = sourceAccessor.ToString();
            
            sourceBinder = new Binder<TSource>();
            sourceBinder
                .BindIf(condition, sourceAccessor)
                .Else(x => default)
                .To((x, v) => Value = v);
            
            sourceBinder
                .BindIf(condition, x => true)
                .Else(x => false)
                .To((x, v) => HasValue = v);

            CanSetValue = CanPrepareSetter(sourceAccessor);
            assignmentActionSupplier = new Lazy<Action<TSource, TProperty>>(() => CanSetValue ? PrepareSetter(sourceAccessor) : throw new NotSupportedException($"Assignment is not supported for {sourceAccessor}"));
            
            this.WhenAnyValue(x => x.Source)
                .WithPrevious()
                .SubscribeSafe(x =>
                {
                    if (sourceBinderAnchors.Disposable != null)
                    {
                        Log.Debug($"Unbinding from existing source {x.Previous}");
                        sourceBinderAnchors.Disposable = default;
                    }
                    Log.Debug($"Binding to {(x.Current == null ? $"NULL of type {typeof(TSource)}" : $"instance {x.Current}")}");
                    sourceBinderAnchors.Disposable = sourceBinder.Attach(x.Current);
                }, Log.HandleUiException)
                .AddTo(Anchors);
        }
        
        public ExpressionWatcher(Expression<Func<TSource, TProperty>> sourceAccessor) : this(sourceAccessor, x => x != null)
        {
        }
        
        private IFluentLog Log { get; }
        
        public string SourceExpression { get; }

        public TProperty Value { get; private set; }

        public string Key { get; protected set; }
        
        public bool HasValue { get; private set; }
        
        public bool CanSetValue { get; }

        public TSource Source { get; set; }
        
        public void SetCurrentValue(object newValue)
        {
            var typedNewValue = (TProperty)newValue;
            SetCurrentValue(typedNewValue);
        }

        public void SetCurrentValue(TProperty newValue)
        {
            if (Source == default)
            {
                throw new InvalidOperationException($"Source is not set, could not set value {newValue}");
            }

            Log.Debug($"Updating value: {Value} => {newValue}");
            assignmentActionSupplier.Value(Source, newValue);
        }

        object IValueWatcher.Value => Value;

        object IValueWatcher.Source
        {
            get => Source;
            set => Source = (TSource)value;
        }

        public override string ToString()
        {
            return $"Watcher: {SourceExpression}";
        }

        private static bool CanPrepareSetter(Expression<Func<TSource, TProperty>> propertyAccessor)
        {
            if (propertyAccessor.Body is not MemberExpression memberExpression)
            {
                return false;
            }
            return memberExpression.Member.MemberType == MemberTypes.Property;
        }

        private static Action<TSource, TProperty> PrepareSetter(Expression<Func<TSource, TProperty>> propertyAccessor)
        {
            if (!CanPrepareSetter(propertyAccessor))
            {
                throw new ArgumentException($"Unsupported expression: {propertyAccessor}");
            }
            var targetExp = propertyAccessor.Parameters[0];
            var valueExp = Expression.Parameter(typeof(TProperty), "v");

            var memberExp = (MemberExpression)propertyAccessor.Body;
            var assign = Expression.Assign(memberExp, valueExp);
            var setter = Binder.ExpressionCompiler.Compile(Expression.Lambda<Action<TSource, TProperty>>(assign, targetExp, valueExp));
            return setter;
        }
    }
}