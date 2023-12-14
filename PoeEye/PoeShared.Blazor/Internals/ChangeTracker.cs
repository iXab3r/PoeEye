using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Blazor.Internals;

internal sealed class ChangeTracker<TContext, TOut> : IChangeTracker where TContext : class
{
    private static readonly IFluentLog Log = typeof(ChangeTracker<TContext, TOut>).PrepareLogger();

    // ReSharper disable once StaticMemberInGenericType
    private static readonly MethodInfo AsEnumerableMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.AsEnumerable))
                                                            ?? throw new MissingMethodException($"Failed to find method {nameof(Enumerable.AsEnumerable)} in {typeof(Enumerable)}");

    private static readonly MethodInfo AttachDependencyAsEnumerableMethod = typeof(ChangeTracker<TContext, TOut>).GetMethod(nameof(AttachDependencyAsEnumerable), BindingFlags.NonPublic | BindingFlags.Static)
                                                                            ?? throw new MissingMethodException($"Failed to find method {nameof(AttachDependencyAsEnumerableMethod)} in {typeof(ChangeTracker<TContext, TOut>)}");

    private static readonly MethodInfo PropagateNullReferencesMethod = typeof(ChangeTracker<TContext, TOut>).GetMethod(nameof(PropagateNullReferences), BindingFlags.NonPublic | BindingFlags.Static)
                                                             ?? throw new MissingMethodException($"Failed to find method {nameof(PropagateNullReferences)} in {typeof(ChangeTracker<TContext, TOut>)}");

    private static readonly MethodInfo PropagateNullablesMethod = typeof(ChangeTracker<TContext, TOut>).GetMethod(nameof(PropagateNullables), BindingFlags.NonPublic | BindingFlags.Static)
                                                            ?? throw new MissingMethodException($"Failed to find method {nameof(PropagateNullables)} in {typeof(ChangeTracker<TContext, TOut>)}");

    
    private readonly TContext context;
    private readonly Expression<Func<TContext, TOut>> selector;
    private readonly CompositeDisposable anchors = new();
    private readonly ISubject<object> whenChanged = new Subject<object>();
    private long revision;
    private TOut value;

    public ChangeTracker(TContext context, Expression<Func<TContext, TOut>> selector)
    {
        if (!IsParameterUsedAtLeastOnce(selector))
        {
            Log.Warn($"The parameter is not used in the expression body: {selector}, expecting that {selector.Body} will use it at least once");
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        this.context = context;
        this.selector = selector;
        StampExpression = selector.ToString();

        var binder = CreateBinder(this, selector);
        binder.Attach(context).AddTo(anchors);

        ChangeTrackerHolder.RecordCreate().AddTo(anchors);
    }

    public TOut Value => value;

    public long Revision => revision;

    public string StampExpression { get; }

    public IObservable<object> WhenChanged => whenChanged;

    private static Binder<TContext> CreateBinder(ChangeTracker<TContext, TOut> parent, Expression<Func<TContext, TOut>> selector)
    {
        var binder = new Binder<TContext>();
        var builder = binder.Bind(selector);

        if (IsReference(typeof(TOut)))
        {
            var propagateNullValues = PropagateNullReferencesMethod.MakeGenericMethod(typeof(TOut), typeof(TContext));
            propagateNullValues.Invoke(null, new object[]{ builder });
        }
        
        if (TryGetNullableType(typeof(TOut), out var innerNullableType))
        {
            var propagateNullValues = PropagateNullablesMethod.MakeGenericMethod(innerNullableType, typeof(TContext));
            propagateNullValues.Invoke(null, new object[]{ builder });
        }
        
        if (IsTypeOrInterfaceOfGenericType(typeof(TOut), typeof(IEnumerable<>)))
        {
            var itemType = GetTypeArgumentOfGenericType(typeof(TOut), typeof(IEnumerable<>));
            var appendMethod = AttachDependencyAsEnumerableMethod.MakeGenericMethod(itemType);
            appendMethod.Invoke(null, new object[] {builder, selector});
        }
        
        var stamp = selector.ToString();
        builder.To((x, v) =>
        {
            var revision = parent.revision++;
            parent.value = v;
            parent.whenChanged.OnNext(new {parent.context, value = v, revision, stamp});
        });
        return binder;
    }

    private static bool IsReference(Type type)
    {
        if (type.IsClass || type.IsInterface)
        {
            return true;
        }

        return false;
    }
    
    private static bool TryGetNullableType(Type type, out Type innerType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            innerType = type.GetGenericArguments()[0];
            return true;
        };
        innerType = null;
        return false;
    }
    
    private static void PropagateNullReferences<TOutput, TOutputContext>(PropertyRuleBuilder<TOutput, TOutputContext> propertyRuleBuilder) 
        where TOutput : class where TOutputContext : class
    {
        propertyRuleBuilder.PropagateNullValues();
    }
    
    private static void PropagateNullables<TOutput, TOutputContext>(PropertyRuleBuilder<TOutput?, TOutputContext> propertyRuleBuilder) 
        where TOutput : struct where TOutputContext : class
    {
        propertyRuleBuilder.PropagateNullValues();
    }
    
    private static void AttachDependencyAsEnumerable<TItem>(PropertyRuleBuilder<TOut, TContext> propertyRuleBuilder, LambdaExpression selector)
    {
        var updatedSelector = AppendAsEnumerable<TItem>(selector);
        propertyRuleBuilder.WithDependency(updatedSelector);
    }

    private static Expression<Func<TContext, IEnumerable<TItem>>> AppendAsEnumerable<TItem>(LambdaExpression selector)
    {
        if (!typeof(IEnumerable<TItem>).IsAssignableFrom(selector.ReturnType))
        {
            throw new ArgumentException($"Selector must return a type assignable to IEnumerable<{typeof(TItem).Name}>", nameof(selector));
        }

        var asEnumerableMethod = AsEnumerableMethod.MakeGenericMethod(typeof(TItem));

        var callAsEnumerable = Expression.Call(
            null,
            asEnumerableMethod,
            selector.Body
        );

        return Expression.Lambda<Func<TContext, IEnumerable<TItem>>>(
            callAsEnumerable,
            selector.Parameters
        );
    }

    private static bool IsTypeOrInterfaceOfGenericType(Type type, Type genericType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
        {
            return true;
        }

        if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType))
        {
            return true;
        }

        return type.BaseType != null && IsTypeOrInterfaceOfGenericType(type.BaseType, genericType);
    }

    private static Type GetTypeArgumentOfGenericType(Type type, Type genericType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
        {
            return type.GetGenericArguments()[0];
        }

        var matchingInterface = type
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType);

        if (matchingInterface != null)
        {
            return matchingInterface.GetGenericArguments()[0];
        }

        if (type.BaseType != null)
        {
            return GetTypeArgumentOfGenericType(type.BaseType, genericType);
        }

        throw new ArgumentException($"The type {type.Name} does not implement or inherit from a generic type {genericType.Name}");
    }

    public void Dispose()
    {
        anchors?.Dispose();
    }

    private static bool IsParameterUsedAtLeastOnce(Expression<Func<TContext, TOut>> expression)
    {
        var visitor = new ParameterUsageVisitor(expression.Parameters.Single());
        visitor.Visit(expression.Body);

        return visitor.ParameterUsed;
    }

    private class ParameterUsageVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression parameter;

        public bool ParameterUsed { get; private set; } = false;

        public ParameterUsageVisitor(ParameterExpression parameter)
        {
            this.parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == parameter)
            {
                ParameterUsed = true;
            }

            return base.VisitParameter(node);
        }
    }
}