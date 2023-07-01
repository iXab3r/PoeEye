using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace PoeShared.Bindings;

public sealed class CsharpExpressionParser : LazyReactiveObject<CsharpExpressionParser>, ICsharpExpressionParser
{
    private static readonly IFluentLog Log = typeof(CsharpExpressionParser).PrepareLogger();

    private static readonly IDynamicLinkCustomTypeProvider DefaultCustomTypeProvider;

    private static readonly ParsingConfig ParsingConfig = new()
    {
        ResolveTypesBySimpleName = true,
        CustomTypeProvider = DefaultCustomTypeProvider,
    };

    static CsharpExpressionParser()
    {
        var provider = new DynamicLinqCustomTypeProvider();
        DefaultCustomTypeProvider = new CachingCustomTypeProvider(provider);
    }

    /// <inheritdoc/>
    public IDynamicLinqCustomTypeProvider CustomTypeProvider => DefaultCustomTypeProvider;

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

    private LambdaExpression ParseLambda<TSource, TResult>(string expression, string parameterName = "x")
    {
        try
        {
            var lambdaExpression = DynamicExpressionParser.ParseLambda(
                ParsingConfig, 
                createParameterCtor: false, 
                parameters: new[] { Expression.Parameter(typeof(TSource), parameterName) },
                resultType: typeof(TResult),
                expression: expression);
            return lambdaExpression;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to parse lambda expression: {expression}", e);
            throw;
        }
    }
}