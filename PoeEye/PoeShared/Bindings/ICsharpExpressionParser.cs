using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace PoeShared.Bindings;

/// <summary>
/// Defines methods to parse C# expressions.
/// </summary>
public interface ICsharpExpressionParser 
{
    /// <summary>
    /// Name of input parameter in lambda expressions, e.g. x => x.ToString(), InputParameterName here is "x"
    /// </summary>
    public const string InputParameterName = "x";
    
    /// <summary>
    /// Interface for providing functionality to find custom types for or resolve any type by full or simple name.
    /// </summary>
    IDynamicLinqCustomTypeProvider CustomTypeProvider { get; }
    
    /// <summary>
    /// Parses a function expression.
    /// </summary>
    /// <typeparam name="TSource">The type of the source parameter.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="expression">The expression string to parse.</param>
    /// <returns>The parsed function expression.</returns>
    Expression<Func<TSource, TResult>> ParseFunction<TSource, TResult>(string expression);
}
