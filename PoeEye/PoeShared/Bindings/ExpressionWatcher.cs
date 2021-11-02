using System;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;
using Binder = PropertyBinder.Binder;

namespace PoeShared.Bindings
{
    public class ExpressionWatcher : ExpressionWatcherBase
    {
        private new static readonly Binder<ExpressionWatcher> Binder;

        static ExpressionWatcher()
        {
            Binder = ExpressionWatcherBase.Binder.Clone<ExpressionWatcher>();
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

        public override string ToString()
        {
            return $"ExpressionWatcher, source: {SourceExpression}, condition: {ConditionExpression}";
        }
    }

    public sealed class ExpressionWatcher<TSource, TProperty> : DisposableReactiveObject, IValueWatcher where TSource : class
    {
        private static readonly Binder<ExpressionWatcher<TSource, TProperty>> Binder = new();

        private readonly Lazy<Action<TSource, TProperty>> assignmentActionSupplier;
        private readonly Expression<Func<TSource, bool>> condition;

        private readonly Expression<Func<TSource, TProperty>> sourceAccessor;
        private readonly Binder<TSource> sourceBinder;
        private readonly SerialDisposable sourceBinderAnchors;

        private readonly string watcherId = $"EW#{Interlocked.Increment(ref ExpressionWatcherHelper.GlobalIdx)}";

        static ExpressionWatcher()
        {
            Binder.Bind(x => x.SupportsSetValue && x.Source != null).To(x => x.CanSetValue);
        }

        public ExpressionWatcher(Expression<Func<TSource, TProperty>> sourceAccessor, Expression<Func<TSource, bool>> condition)
        {
            Log = typeof(ExpressionWatcher<TSource, TProperty>).PrepareLogger("ExpressionWatcher").WithSuffix(watcherId).WithSuffix(ToString);
            Log.Debug($"Expression created, source: {sourceAccessor}, condition: {condition}");

            this.sourceAccessor = sourceAccessor;
            this.condition = condition;
            sourceBinderAnchors = new SerialDisposable().AddTo(Anchors);
            SourceExpression = sourceAccessor.ToString();
            
            sourceBinder = new Binder<TSource>().WithExceptionHandler(HandleBinderException);
            sourceBinder
                .BindIf(condition, sourceAccessor)
                .Else(x => default)
                .To((x, v) =>
                {
                    Error = default;
                    Value = v;
                });
            
            sourceBinder
                .BindIf(condition, x => true)
                .Else(x => false)
                .To((x, v) => HasValue = v);

            SupportsSetValue = CanPrepareSetter(sourceAccessor);
            assignmentActionSupplier = new Lazy<Action<TSource, TProperty>>(() => SupportsSetValue ? PrepareSetter(sourceAccessor) : throw new NotSupportedException($"Assignment is not supported for {sourceAccessor}"));
            
            this.WhenAnyValue(x => x.Source)
                .WithPrevious()
                .SubscribeSafe(x =>
                {
                    if (sourceBinderAnchors.Disposable != null && x.Previous != null)
                    {
                        Log.Debug($"Unbinding from existing source {x.Previous}");
                        sourceBinderAnchors.Disposable = default;
                    }

                    if (x.Current != null)
                    {
                        Log.Debug($"Binding to source {x.Current}");
                    }
                    Error = default;
                    sourceBinderAnchors.Disposable = sourceBinder.Attach(x.Current);
                }, Log.HandleUiException)
                .AddTo(Anchors);

            Disposable.Create(() =>
            {
                Log.Debug("Instance disposed, resetting all values");
                CanSetValue = false;
                Value = default;
                HasValue = false;
                Source = default;
            }).AddTo(Anchors);
            
            Binder.Attach(this).AddTo(Anchors);
            Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
        }

        public ExpressionWatcher(Expression<Func<TSource, TProperty>> sourceAccessor) : this(sourceAccessor, x => x != null)
        {
        }

        private IFluentLog Log { get; }

        public string SourceExpression { get; }

        public TProperty Value { get; private set; }

        public TSource Source { get; set; }

        public Exception Error { get; private set; }

        public bool HasValue { get; private set; }

        public bool CanSetValue { get; private set; }

        public bool SupportsSetValue { get; }

        object IValueProvider.Value => Value;

        object IValueWatcher.Source
        {
            get => Source;
            set => Source = (TSource)value;
        }

        public void SetCurrentValue(object newValue)
        {
            if (newValue != null && newValue is not TProperty)
            {
                var error = new BindingException($"Failed to set current value value of {Source} from {Value} to {newValue} - invalid property type, expected {typeof(TProperty)}, got {newValue.GetType()}");
                Log.Warn($"Could not set current value of watcher {this}", error);
                Error = error;
                return;
            }
            
            var typedNewValue = newValue == null ? default : (TProperty)newValue;
            SetCurrentValue(typedNewValue);
        }

        private void HandleBinderException(object sender, ExceptionEventArgs e)
        {
            Error = e.Exception;
            e.Handled = true;
            Log.Warn($"Binder encountered exception: {e}", e.Exception);
        }

        public void SetCurrentValue(TProperty newValue)
        {
            if (Source == default)
            {
                throw new InvalidOperationException($"Source is not set, could not set value {newValue}");
            }

            Log.Debug($"Updating value: {Value} => {newValue}");
            try
            {
                Error = default;
                assignmentActionSupplier.Value(Source, newValue);
            }
            catch (Exception e)
            {
                Error = new BindingException($"Failed to set value {newValue}, watcher: {this} - {e}", e);
                Log.Warn($"Failed to change value of {Source} from {Value} to {newValue}", e);
            }
        }

        public override string ToString()
        {
            return $"EW: {SourceExpression}, source: {(Source == default ? "not set" : Source.ToString())}, {(HasValue ? $"value: {Value}" : $"hasValue: {HasValue}")}";
        }

        private static bool CanPrepareSetter(Expression<Func<TSource, TProperty>> propertyAccessor)
        {
            if (propertyAccessor.Body is not MemberExpression memberExpression)
            {
                return false;
            }

            if (memberExpression.Member is not PropertyInfo propertyInfo)
            {
                return false;
            }

            return propertyInfo.CanWrite;
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
            var setter = PropertyBinder.Binder.ExpressionCompiler.Compile(Expression.Lambda<Action<TSource, TProperty>>(assign, targetExp, valueExp));
            return setter;
        }

      
    }
}