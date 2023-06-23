using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Reflection;

namespace PoeShared.Bindings;

internal sealed class ExpressionParser : LazyReactiveObject<ExpressionParser>, IExpressionParser
{
    private static readonly IFluentLog Log = typeof(ExpressionParser).PrepareLogger();

    private static readonly PassthroughLinkCustomTypeProvider CustomTypeProvider = new();

    private readonly ParsingConfig parsingConfig;

    public ExpressionParser()
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
        var parameterName = ExtractParameterName(expression);
        return ParseFunction<TSource, TResult>(expression, parameterName);
    }

    private LambdaExpression ParseLambda<TSource, TResult>(string expression, string parameterName)
    {
        try
        {
            var sourceParameter = Expression.Parameter(typeof(TSource), parameterName);
            var lambdaExpression = DynamicExpressionParser.ParseLambda<TSource, TResult>(parsingConfig, createParameterCtor: false, expression: expression, sourceParameter);
            return lambdaExpression;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to parse lambda expression: {expression}", e);
            throw;
        }
    }

    private Expression<Func<TSource, TResult>> ParseFunction<TSource, TResult>(string expression, string parameterName)
    {
        var lambdaExpression = ParseLambda<TSource, TResult>(expression, parameterName);

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

    private static string ExtractParameterName(string expression)
    {
        var parametersPartIdx = expression.IndexOf("=>", StringComparison.Ordinal);
        if (parametersPartIdx < 0)
        {
            throw new ArgumentException($"Failed to extract parameter name from expression {expression}");
        }

        var parametersPart = expression.Substring(0, parametersPartIdx);

        var result = parametersPart.Trim('(', ' ', ')');
        return result;
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