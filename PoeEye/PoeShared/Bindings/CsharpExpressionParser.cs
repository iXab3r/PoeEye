using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Reflection;

namespace PoeShared.Bindings;

public sealed class CsharpExpressionParser : LazyReactiveObject<CsharpExpressionParser>, ICsharpExpressionParser
{
    private static readonly IFluentLog Log = typeof(CsharpExpressionParser).PrepareLogger();

    private static readonly PassthroughLinkCustomTypeProvider CustomTypeProvider = new();

    private readonly ParsingConfig parsingConfig;

    public CsharpExpressionParser()
    {
        parsingConfig = new ParsingConfig
        {
            ResolveTypesBySimpleName = true,
            CustomTypeProvider = CustomTypeProvider
        };
    }

    /// <inheritdoc/>
    public Expression<Func<TSource, TResult>> ParseFunction<TSource, TResult>(string expression)
    {
        var lambdaExpression = ParseLambda<TSource, TResult>(expression);
        try
        {
            var result = (Expression<Func<TSource, TResult>>) lambdaExpression;
            return result;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to convert lambda expression to {typeof(Expression<Func<TSource, TResult>>)}: {lambdaExpression}", e);
            throw;
        }
    }

    private LambdaExpression ParseLambda<TSource, TResult>(string expression)
    {
        try
        {
            var lambdaExpression = DynamicExpressionParser.ParseLambda<TSource, TResult>(parsingConfig, createParameterCtor: false, expression: expression);
            return lambdaExpression;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to parse lambda expression: {expression}", e);
            throw;
        }
    }
    
    private sealed class PassthroughLinkCustomTypeProvider : IDynamicLinkCustomTypeProvider
    {
        private readonly IDynamicLinkCustomTypeProvider fallback;

        public PassthroughLinkCustomTypeProvider()
        {
            fallback = new DynamicLinqCustomTypeProvider();
        }

        public HashSet<Type> GetCustomTypes()
        {
            return fallback.GetCustomTypes();
        }

        public Dictionary<Type, List<MethodInfo>> GetExtensionMethods()
        {
            return fallback.GetExtensionMethods();
        }

        public Type ResolveType(string typeName)
        {
            return fallback.ResolveType(typeName);
        }

        public Type ResolveTypeBySimpleName(string simpleTypeName)
        {
            return fallback.ResolveTypeBySimpleName(simpleTypeName);
        }
    }
}