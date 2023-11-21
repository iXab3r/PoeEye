using DynamicData;

namespace PoeShared.Modularity;

/// <summary>
/// Provides properties and methods to track and report errors.
/// </summary>
public interface IHasErrors
{
    /// <summary>
    /// Gets the last error that occurred.
    /// </summary>
    ErrorInfo? LastError { get; }
    
    /// <summary>
    /// Gets a value indicating whether any errors have been reported.
    /// </summary>
    bool HasErrors { get; }
    
    /// <summary>
    /// Gets a list of all reported errors.
    /// </summary>
    IObservableList<ErrorInfo> Errors { get; }
}