using System.Reflection;

namespace PoeShared;

[DebuggerStepThrough]
public static class Guard
{
    /// <summary>
    /// Checks if the given <paramref name="type"/> is an interface type.
    /// </summary>
    /// <exception cref="ArgumentException">The <paramref name="type" /> parameter is not an interface type.</exception>
    public static void ArgumentMustBeInterface([ValidatedNotNull]Type type)
    {
        CheckIfTypeIsInterface(type, false, ExceptionMessages.ArgumentMustBeInterface);
    }

    /// <summary>
    /// Checks if the given <paramref name="type"/> is not an interface type.
    /// </summary>
    /// <exception cref="ArgumentException">The <paramref name="type" /> parameter is an interface type.</exception>
    public static void ArgumentMustNotBeInterface([ValidatedNotNull]Type type)
    {
        CheckIfTypeIsInterface(type, true, ExceptionMessages.ArgumentMustNotBeInterface);
    }

    private static void CheckIfTypeIsInterface(Type type, bool throwIfItIsAnInterface, string exceptionMessage)
    {
        ArgumentNotNull(type, nameof(type));

        if (type.GetTypeInfo().IsInterface == throwIfItIsAnInterface)
        {
            throw new ArgumentException(exceptionMessage, type.Name);
        }
    }
        
    /// <summary>
    /// Checks if the given <paramref name="value"/> is true.
    /// </summary>
    /// <exception cref="ArgumentException">The <paramref name="value" /> parameter is false.</exception>
    public static void ArgumentIsTrue(bool value, string paramName)
    {
        if (!value)
        {
            throw new ArgumentException(ExceptionMessages.ArgumentMustBeTrue, paramName);
        }
    }

    /// <summary>
    /// Checks if the given <paramref name="value"/> is false.
    /// </summary>
    /// <exception cref="ArgumentException">The <paramref name="value" /> parameter is true.</exception>
    public static void ArgumentIsFalse(bool value, string paramName)
    {
        if (value)
        {
            throw new ArgumentException(ExceptionMessages.ArgumentMustBeFalse, paramName);
        }
    }

    /// <summary>
    /// Checks if the given <paramref name="expression"/> is true.
    /// </summary>
    /// <exception cref="ArgumentException">The <paramref name="expression" /> parameter is false.</exception>
    public static void ArgumentIsTrue([ValidatedNotNull]Expression<Func<bool>> expression)
    {
        ArgumentIsTrueOrFalse(expression, throwCondition: false, exceptionMessage: ExceptionMessages.ArgumentMustBeTrue);
    }

    /// <summary>
    /// Checks if the given <paramref name="expression"/> is false.
    /// </summary>
    /// <exception cref="ArgumentException">The <paramref name="expression" /> parameter is true.</exception>
    public static void ArgumentIsFalse([ValidatedNotNull]Expression<Func<bool>> expression)
    {
        ArgumentIsTrueOrFalse(expression, throwCondition: true, exceptionMessage: ExceptionMessages.ArgumentMustBeFalse);
    }

    private static void ArgumentIsTrueOrFalse(Expression<Func<bool>> expression, bool throwCondition, string exceptionMessage)
    {
        ArgumentNotNull(expression, nameof(expression));

        if (expression.Compile().Invoke() == throwCondition)
        {
            var paramName = expression.GetMemberName();
            throw new ArgumentException(exceptionMessage, paramName);
        }
    }
        
    /// <summary>
    ///     Checks if the given value meets the given condition.
    /// </summary>
    /// <example>
    ///     Only pass single parameters through to this call via expression, e.g. Guard.ArgumentCondition(() => value, v => true)
    /// </example>
    public static void ArgumentCondition<T>([ValidatedNotNull]Expression<Func<T>> expression, Expression<Func<T, bool>> condition)
    {
        ArgumentNotNull(expression, nameof(expression));

        var propertyValue = expression.Compile()();
        var paramName = expression.GetMemberName();

        ArgumentCondition(propertyValue, paramName, condition);
    }

    /// <summary>
    ///     Checks if the given value meets the given condition.
    /// </summary>
    /// <example>
    ///     Only pass single parameters through to this call via expression, e.g. Guard.ArgumentCondition(value, "value", v => true)
    /// </example>
    public static void ArgumentCondition<T>([ValidatedNotNull]T value, string paramName, Expression<Func<T, bool>> condition)
    {
        ArgumentNotNull(condition, paramName);

        if (!condition.Compile()(value))
        {
            throw new ArgumentException(string.Format(ExceptionMessages.ArgumentCondition, condition), paramName);
        }
    }
        
    /// <summary>
    ///     Checks if the given string is not null or empty.
    /// </summary>
    public static void ArgumentNotNullOrEmpty([ValidatedNotNull]Expression<Func<IEnumerable>> expression)
    {
        ArgumentNotNull(expression, nameof(expression));

        var propertyValue = expression.Compile()();
        var paramName = expression.GetMemberName();

        ArgumentNotNullOrEmpty(propertyValue, paramName);
    }

    /// <summary>
    ///     Checks if the given string is not null or empty.
    /// </summary>
    public static void ArgumentNotNullOrEmpty([ValidatedNotNull]IEnumerable enumerable, string paramName)
    {
        ArgumentNotNull(enumerable, paramName);

        bool hasElement = enumerable.GetEnumerator().MoveNext();
        if (!hasElement)
        {
            throw new ArgumentException(ExceptionMessages.ArgumentMustNotBeEmpty, paramName);
        }
    }
        
    /// <summary>
    ///     Checks if the given value is not null.
    /// </summary>
    /// <example>
    ///     Only pass single parameters through to this call via expression, e.g. Guard.ArgumentNull(() => someParam)
    /// </example>
    /// <param name="expression">An expression containing a single string parameter e.g. () => someParam</param>
    public static void ArgumentNull<T>([ValidatedNotNull]Expression<Func<T>> expression)
    {
        ArgumentNotNull(expression, nameof(expression));

        var propertyValue = expression.Compile()();
        var paramName = expression.GetMemberName();

        ArgumentNull(propertyValue, paramName);
    }

    /// <summary>
    ///     Checks if the given value is not null.
    /// </summary>
    /// <example>
    ///     Pass the parameter and it's name, e.g. Guard.ArgumentNull(someParam, nameof(someParam))
    /// </example>
    public static void ArgumentNull<T>([ValidatedNotNull]T value, string paramName)
    {
        if (value != null)
        {
            throw new ArgumentException(ExceptionMessages.ArgumentMustBeNull, paramName);
        }
    }

    /// <summary>
    ///     Checks if the given value is not null.
    /// </summary>
    /// <example>
    ///     Only pass single parameters through to this call via expression, e.g. Guard.ArgumentNotNull(() => someParam)
    /// </example>
    /// <param name="expression">An expression containing a single string parameter e.g. () => someParam</param>
    public static void ArgumentNotNull<T>([ValidatedNotNull]Expression<Func<T>> expression)
    {
        ArgumentNotNull(expression, nameof(expression));

        var propertyValue = expression.Compile()();
        var paramName = expression.GetMemberName();

        ArgumentNotNull(propertyValue, paramName);
    }

    /// <summary>
    ///     Checks if the given value is not null.
    /// </summary>
    /// <example>
    ///     Pass the parameter and it's name, e.g. Guard.ArgumentNotNull(someParam, nameof(someParam))
    /// </example>
    public static void ArgumentNotNull<T>([ValidatedNotNull]T value, string paramName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(paramName, ExceptionMessages.ArgumentMustNotBeNull);
        }
    }
        
    /// <summary>
    /// Checks if given argument is greater than given value.
    /// </summary>
    /// <param name="expression">Given argument</param>
    /// <param name="givenValue">Given value.</param>        
    public static void ArgumentIsGreaterThan<T>([ValidatedNotNull]Expression<Func<T>> expression, T givenValue) where T : struct, IComparable<T>
    {
        ArgumentNotNull(expression);

        var propertyValue = expression.Compile()();
        if (propertyValue.IsLessThanOrEqual(givenValue))
        {
            var paramName = expression.GetMemberName();
            throw new ArgumentOutOfRangeException(paramName, propertyValue, string.Format(ExceptionMessages.ArgumentIsGreaterThan, givenValue));
        }
    }

    /// <summary>
    /// Checks if given argument is greater or equal to given value.
    /// </summary>
    /// <param name="argument">Given argument</param>
    /// <param name="givenValue">Given value.</param>   
    public static void ArgumentIsGreaterOrEqual<T>([ValidatedNotNull]Expression<Func<T>> argument, T givenValue) where T : struct, IComparable<T>
    {
        ArgumentNotNull(argument);

        var propertyValue = argument.Compile()();
        if (propertyValue.IsLessThan(givenValue))
        {
            var paramName = ((MemberExpression)argument.Body).Member.Name;
            throw new ArgumentOutOfRangeException(paramName, propertyValue, string.Format(ExceptionMessages.ArgumentIsGreaterOrEqual, givenValue));
        }
    }

    /// <summary>
    /// Checks if given argument is lower than given value.
    /// </summary>
    /// <param name="argument">Given argument</param>
    /// <param name="givenValue">Given value.</param>   
    public static void ArgumentIsLowerThan<T>([ValidatedNotNull]Expression<Func<T>> argument, T givenValue) where T : struct, IComparable<T>
    {
        ArgumentNotNull(argument);

        var propertyValue = argument.Compile()();
        if (propertyValue.IsGreaterOrEqual(givenValue))
        {
            var paramName = ((MemberExpression)argument.Body).Member.Name;
            throw new ArgumentOutOfRangeException(paramName, propertyValue, string.Format(ExceptionMessages.ArgumentIsLowerThan, givenValue));
        }
    }

    /// <summary>
    /// Checks if given argument is lower or equal to given value.
    /// </summary>
    /// <param name="argument">Given argument</param>
    /// <param name="givenValue">Given value.</param>   
    public static void ArgumentIsLowerOrEqual<T>([ValidatedNotNull]Expression<Func<T>> argument, T givenValue) where T : struct, IComparable<T>
    {
        ArgumentNotNull(argument);

        var propertyValue = argument.Compile()();
        if (propertyValue.IsGreaterThan(givenValue))
        {
            var paramName = ((MemberExpression)argument.Body).Member.Name;
            throw new ArgumentOutOfRangeException(paramName, propertyValue, string.Format(ExceptionMessages.ArgumentIsLowerOrEqual, givenValue));
        }
    }

    /// <summary>
    /// Checks if given argument is between given lower value and upper value.
    /// </summary>
    /// <param name="argument">Given argument</param>
    /// <param name="lowerBound"></param>
    /// <param name="upperBound"></param>
    /// <param name="inclusive">Inclusive lower bound value if <param name="inclusive">true</param>.</param>   
    public static void ArgumentIsBetween<T>([ValidatedNotNull]Expression<Func<T>> argument, T lowerBound, T upperBound, bool inclusive = false) where T : struct, IComparable<T>
    {
        ArgumentNotNull(argument);

        var propertyValue = argument.Compile()();
        if (!propertyValue.IsBetween(lowerBound, upperBound, inclusive))
        {
            var paramName = ((MemberExpression)argument.Body).Member.Name;
            throw new ArgumentOutOfRangeException(paramName, propertyValue, string.Format(ExceptionMessages.ArgumentIsBetween, inclusive ? "(" : "[", lowerBound, upperBound, inclusive ? ")" : "]"));
        }
    }

    /// <summary>
    ///     Verifies the <paramref name="expression" /> is not a negative number and throws an
    ///     <see cref="ArgumentOutOfRangeException" /> if it is a negative number.
    /// </summary>
    /// <param name="expression">An expression containing a single parameter e.g. () => param</param>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="expression" /> parameter is a negative number.</exception>
    public static void ArgumentIsNotNegative<T>([ValidatedNotNull]Expression<Func<T>> expression) where T : struct, IComparable<T>
    {
        ArgumentNotNull(expression);

        var argumentValue = expression.Compile()();
        ArgumentIsNotNegative(argumentValue, expression.GetMemberName());
    }

    /// <summary>
    ///     Checks if <paramref name="argumentValue" /> is not a negative number.
    /// </summary>
    /// <param name="argumentValue">The value to verify.</param>
    /// <param name="argumentName">The name of the <paramref name="argumentValue" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="argumentValue" /> parameter is a negative number.</exception>
    public static void ArgumentIsNotNegative<T>(T argumentValue, string argumentName) where T : struct, IComparable<T>
    {
        if (argumentValue.IsLessThan(default(T)))
        {
            throw new ArgumentOutOfRangeException(argumentName, argumentValue, string.Format(CultureInfo.InvariantCulture, ExceptionMessages.ArgumentIsNotNegative));
        }
    }
        
    /// <summary>
    ///     Checks if the given string is not null or empty.
    /// </summary>
    public static void ArgumentNotNullOrEmpty([ValidatedNotNull]Expression<Func<string>> expression)
    {
        ArgumentNotNull(expression, nameof(expression));

        var propertyValue = expression.Compile()();
        var paramName = expression.GetMemberName();

        ArgumentNotNullOrEmpty(propertyValue, paramName);
    }

    /// <summary>
    ///     Checks if the given string is not null or empty.
    /// </summary>
    public static void ArgumentNotNullOrEmpty([ValidatedNotNull]string propertyValue, string paramName)
    {
        if (string.IsNullOrEmpty(propertyValue))
        {
            ArgumentNotNull(propertyValue, paramName);

            throw new ArgumentException(ExceptionMessages.ArgumentMustNotBeEmpty, paramName);
        }
    }

    /// <summary>
    /// Checks if the given string has the expected length
    /// </summary>
    /// <param name="expression">Property expression.</param>
    /// <param name="expectedLength">Expected length.</param>
    public static void ArgumentHasLength([ValidatedNotNull]Expression<Func<string>> expression, int expectedLength)
    {
        ArgumentNotNull(expression, nameof(expression));

        var propertyValue = expression.Compile()();
        int length = propertyValue.Length;
        if (length != expectedLength)
        {
            var paramName = expression.GetMemberName();
            throw new ArgumentException(string.Format(ExceptionMessages.ArgumentHasLength, expectedLength, length), paramName);
        }
    }

    /// <summary>
    /// Checks if the given string has the expected length
    /// </summary>
    /// <param name="propertyValue">Property value.</param>
    /// <param name="paramName">Parameter name.</param>
    /// <param name="expectedLength">Expected length.</param>
    public static void ArgumentHasLength([ValidatedNotNull]string propertyValue, string paramName, int expectedLength)
    {
        ArgumentNotNull(propertyValue, paramName);

        int length = propertyValue.Length;
        if (length != expectedLength)
        {
            throw new ArgumentException(string.Format(ExceptionMessages.ArgumentHasLength, expectedLength, length), paramName);
        }
    }

    /// <summary>
    /// Checks if the given string has a length which exceeds given max length.
    /// </summary>
    public static void ArgumentHasMaxLength([ValidatedNotNull]Expression<Func<string>> expression, int maxLength)
    {
        ArgumentNotNull(expression, nameof(expression));

        var propertyValue = expression.Compile()();
        var paramName = expression.GetMemberName();

        ArgumentHasMaxLength(propertyValue, paramName, maxLength);
    }

    /// <summary>
    /// Checks if the given string has a length which exceeds given max length.
    /// </summary>
    public static void ArgumentHasMaxLength([ValidatedNotNull]string propertyValue, string paramName, int maxLength)
    {
        ArgumentNotNull(propertyValue, paramName);

        int length = propertyValue.Length;
        if (length > maxLength)
        {
            throw new ArgumentException(string.Format(ExceptionMessages.ArgumentHasMaxLength, maxLength, length), paramName);
        }
    }

    /// <summary>
    /// Checks if the given string has a length which is at least given min length long.
    /// </summary>
    public static void ArgumentHasMinLength([ValidatedNotNull]Expression<Func<string>> expression, int minLength)
    {
        ArgumentNotNull(expression, nameof(expression));

        var propertyValue = expression.Compile()();
        var paramName = expression.GetMemberName();

        ArgumentHasMinLength(propertyValue, paramName, minLength);
    }

    /// <summary>
    /// Checks if the given string has a length which is at least given min length long.
    /// </summary>
    public static void ArgumentHasMinLength([ValidatedNotNull]string propertyValue, string paramName, int minLength)
    {
        ArgumentNotNull(propertyValue, paramName);

        var length = propertyValue.Length;
        if (length < minLength)
        {
            throw new ArgumentException(string.Format(ExceptionMessages.ArgumentHasMinLength, minLength, length), paramName);
        }
    }

    public static void ArgumentMustBeOfType<T>(object value, string paramName)
    {
        ArgumentNotNull(value, nameof(value));
        if (!(value is T))
        {
            throw new ArgumentException($"Value must be of type {typeof(T)}, but was {value.GetType()}");
        }
    }
        
    private static class ExceptionMessages
    {
        public const string ArgumentMustBeInterface = "Type must be an interface.";
        public const string ArgumentMustNotBeInterface = "Type must not be an interface.";

        public const string ArgumentMustNotBeNull = "Argument must not be null.";
        public const string ArgumentMustBeNull = "Argument must be null.";

        public const string ArgumentMustBeTrue = "Argument must be true.";
        public const string ArgumentMustBeFalse = "Argument must be false.";

        public const string ArgumentIsGreaterThan = "Argument must be greater than {0}.";
        public const string ArgumentIsGreaterOrEqual = "Argument must be greater or equal to {0}.";
        public const string ArgumentIsLowerThan = "Argument must be lower than {0}.";
        public const string ArgumentIsLowerOrEqual = "Argument must be lower or equal to {0}.";
        public const string ArgumentIsBetween = "Argument must be between {0}{1} and {2}{3}.";
        public const string ArgumentIsNotNegative = "Provided number must not be negative.";

        public const string ArgumentMustNotBeEmpty = "Argument must not be empty.";
        public const string ArgumentHasLength = "Expected string length is {0}, but found {1}.";
        public const string ArgumentHasMaxLength = "String length exceeds maximum of {0} characters. Found string of length {1}.";
        public const string ArgumentHasMinLength = "String must have a minimum of {0} characters. Found string of length {1}.";

        public const string ArgumentCondition = "Given condition \"{0}\" is not met.";
    }
        
    /// <summary>
    /// ValidatedNotNullAttribute signals to static code analysis (CA1062)
    /// to trust that we're really checking the marked parameters for null references.
    /// </summary>
    private sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}