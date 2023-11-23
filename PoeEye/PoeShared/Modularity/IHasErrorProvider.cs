namespace PoeShared.Modularity;

/// <summary>
/// Defines a contract for an object that provides error handling capabilities.
/// </summary>
public interface IHasErrorProvider
{
    /// <summary>
    /// Gets the <see cref="ICanSetErrors"/> instance used for error reporting and management.
    /// </summary>
    /// <remarks>
    /// This property provides access to an error provider that allows reporting,
    /// subscribing to, and clearing errors. It extends the functionality of <see cref="IHasErrors"/>
    /// by adding methods to actively manage and report errors.
    /// </remarks>
    ICanSetErrors ErrorProvider { get; }
}
