using System;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using PropertyBinder;
using Binder = PropertyBinder.Binder;

namespace PoeShared.Scaffolding
{
    public sealed class BinderConfig : DisposableReactiveObject
    {

        public BinderConfig(
            BindableReactiveObject owner,
            DisposableReactiveObject source, 
            string sourcePath, 
            DisposableReactiveObject target, 
            string targetPropertyName)
        {
            Owner = owner;
            Source = source;
            Target = target;
            SourcePath = sourcePath;
            TargetPropertyName = targetPropertyName;
            
            var targetType = Target.GetType();
            var targetProperty = targetType.GetProperty(TargetPropertyName, BindingFlags.Public | BindingFlags.Instance) ?? throw new ArgumentException($"Failed to find property {TargetPropertyName} on type {targetType}");
            TargetPropertyType = targetProperty.PropertyType;

            Attach().AddTo(Anchors);
        }

        public BindableReactiveObject Owner { get; }

        public DisposableReactiveObject Source { get; }

        public DisposableReactiveObject Target { get; }

        public string SourcePath { get; }

        public string TargetPropertyName { get; }

        public Type TargetPropertyType { get; }

        public object PropertyValue { get; private set; }

        private IDisposable Attach()
        {
            var typeArgs = new[] { Source.GetType(), Target.GetType(), TargetPropertyType };

            var attachMethod = GetType().GetMethod(nameof(Attach), BindingFlags.Static | BindingFlags.NonPublic) ?? throw new ArgumentException($"Failed to find attach method");
            var genericAttach = attachMethod.MakeGenericMethod(typeArgs);
            
            var anchor = (IDisposable)genericAttach.Invoke(null, new object[] { this, Source, SourcePath, Target, TargetPropertyName });
            return anchor;
        }

        private static IDisposable Attach<TSource, TTarget, TSourceType>(
            BinderConfig owner,
            TSource source, 
            string sourcePath, 
            TTarget target, 
            string targetPath) where TSource : class
        {
            var binder = new Binder<TSource>();
                
            var sourceExprText = $@"x => x.{sourcePath}";
            var sourceParameter = Expression.Parameter(typeof(TSource), "x");
            var sourceBinderExpr = DynamicExpressionParser.ParseLambda<TSource, TSourceType>(ParsingConfig.Default, false, sourceExprText, sourceParameter);
                
            var targetProperty = typeof(TTarget).GetProperty(targetPath) ?? throw new ArgumentException($"Failed to find property {targetPath} on {target}");
            var targetExp = Expression.Parameter(typeof(TTarget), "x");
            var valueExp = Expression.Parameter(typeof(TSourceType), "v");

            var propertyAccessor = Expression.Property(targetExp, targetProperty);
            var assign = Expression.Assign(propertyAccessor, valueExp);
            var setter = Binder.ExpressionCompiler.Compile(Expression.Lambda<Action<TTarget, TSourceType>>(assign, targetExp, valueExp));
                
            binder.Bind(sourceBinderExpr).To((x, v) =>
            {
                setter(target, v);
                owner.PropertyValue = v;
            });
            return binder.Attach(source);
        }
    }
}