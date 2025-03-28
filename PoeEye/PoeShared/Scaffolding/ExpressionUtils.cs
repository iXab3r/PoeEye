namespace PoeShared.Scaffolding;

public static class ExpressionUtils
{
    /// <summary>
    /// Parses an expression representing access to a nested property and returns two components:
    /// 1. A lambda expression that evaluates the root object involved in the property chain.
    /// 2. A string representing the property path relative to that root object (excluding the root itself).
    /// </summary>
    /// <typeparam name="T">
    /// The type of the final property being accessed in the expression chain.
    /// </typeparam>
    /// <param name="targetProperty">
    /// An expression representing the property access to be parsed.
    /// For example: <c>() => DataContext.Mouse.X</c>
    /// </param>
    /// <returns>
    /// A tuple containing:
    /// - <c>rootExpression</c>: a lambda expression that returns the root object (e.g., <c>DataContext</c>).
    /// - <c>propertyPath</c>: a string representing the dot-separated property path relative to the root
    ///   (e.g., <c>Mouse.X</c>).
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the expression structure is unsupported, such as:
    /// - a constant expression with no parent,
    /// - a property chain that doesn't contain at least two member expressions,
    /// - or an unknown expression type that cannot be resolved.
    /// </exception>
    /// <example>
    /// Suppose you have the following object hierarchy:
    /// <code>
    /// public class AppModel {
    ///     public MouseState Mouse { get; set; }
    /// }
    /// 
    /// public class MouseState {
    ///     public int X { get; set; }
    ///     public int Y { get; set; }
    /// }
    /// 
    /// AppModel DataContext = new AppModel {
    ///     Mouse = new MouseState { X = 100, Y = 200 }
    /// };
    /// </code>
    ///
    /// And you call:
    /// <code>
    /// var (rootExpr, path) = ParseExpression(() => DataContext.Mouse.X);
    /// </code>
    ///
    /// Then:
    /// - <c>rootExpr()</c> will evaluate to <c>DataContext</c>
    /// - <c>path</c> will be <c>"Mouse.X"</c>
    /// </example>
    public static (Expression<Func<object>> rootExpression, string propertyPath) ParseBindingExpression<T>(Expression<Func<T>> targetProperty)
    {
        var parsed = new List<Expression>();
        Expression current = targetProperty;

        while (true)
        {
            parsed.Add(current);

            if (current is LambdaExpression lambdaExpression)
            {
                current = lambdaExpression.Body;
            }
            else if (current is MemberExpression memberExpression)
            {
                current = memberExpression.Expression ?? throw new InvalidOperationException(
                    $"Unsupported expression structure - inner member expression of {memberExpression} is not set in {targetProperty}");
            }
            else if (current is ConstantExpression constantExpression)
            {
                // Reached the root of the expression

                if (parsed.Count < 2)
                {
                    throw new InvalidOperationException(
                        $"Unsupported expression structure - constant expression {constantExpression} does not have parent node in {targetProperty}");
                }

                if (parsed.Count < 3)
                {
                    throw new InvalidOperationException(
                        $"Unsupported expression structure - there is no property path in {targetProperty}");
                }

                var accessor = parsed[^2]; // This accesses the root object
                var fullPropertyPath = BuildPropertyPath((MemberExpression) parsed[1]); // Full path: e.g., DataContext.Mouse.X
                var propertyPath = fullPropertyPath
                    .Split(".", StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1)
                    .JoinStrings("."); // Strip the root: Mouse.X

                var rootExpression = Expression.Lambda<Func<object>>(
                    Expression.Convert(accessor, typeof(object))
                );

                return (rootExpression, propertyPath);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported expression structure - unknown expression {current} in {targetProperty}");
            }
        }
    }

    /// <summary>
    /// Builds a full dot-separated string representing a property access chain,
    /// e.g., for the expression <c>DataContext.Mouse.X</c>, returns <c>DataContext.Mouse.X</c>.
    /// </summary>
    /// <param name="memberExpression">The member expression representing the leaf node.</param>
    /// <returns>A string representing the full property access path.</returns>
    private static string BuildPropertyPath(MemberExpression memberExpression)
    {
        if (memberExpression.Expression is MemberExpression innerMember)
        {
            return BuildPropertyPath(innerMember) + "." + memberExpression.Member.Name;
        }

        return memberExpression.Member.Name;
    }
}