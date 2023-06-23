namespace PoeShared.Bindings;

/// <summary>
/// Defines methods to parse C# expressions.
/// </summary>
public interface IExpressionParser
{
    /// <summary>
    /// Parses a function expression.
    /// </summary>
    /// <typeparam name="TSource">The type of the source parameter.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="expression">The expression string to parse.</param>
    /// <returns>The parsed function expression.</returns>
    Expression<Func<TSource, TResult>> ParseFunction<TSource, TResult>(string expression);
}
