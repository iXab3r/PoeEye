namespace PoeShared.Modularity;

/// <summary>
/// Represents exceptions that occur during the processing of Poe configuration.
/// </summary>
public sealed class PoeConfigException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PoeConfigException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public PoeConfigException(string message, Exception inner) : base(message, inner)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PoeConfigException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public PoeConfigException(string message) : base(message)
    {
    }

    /// <summary>
    /// Gets the value that is being deserialized or serialized when the exception occurred.
    /// </summary>
    /// <value>The current value involved in the serialization or deserialization process.</value>
    public object Value { get; init; }
    
    /// <summary>
    /// Gets the metadata used for serialization or deserialization when the exception occurred.
    /// </summary>
    /// <value>The metadata associated with the current serialization or deserialization process.</value>
    public PoeConfigMetadata Metadata { get; init; }
    
    /// <summary>
    /// Gets the specific type of exception that occurred during the Poe configuration processing.
    /// </summary>
    /// <value>The type of exception as defined in <see cref="PoeConfigExceptionType"/>.</value>
    public PoeConfigExceptionType ExceptionType { get; init; }

    public int MaxSupportedVersion { get; init; }
}
