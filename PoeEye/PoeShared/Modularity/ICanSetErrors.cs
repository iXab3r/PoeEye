using DynamicData;

namespace PoeShared.Modularity;

/// <summary>
/// Extends <see cref="IHasErrors"/> with methods to report and clear errors.
/// </summary>
public interface ICanSetErrors : IHasErrors
{
    /// <summary>
    /// Reports an error.
    /// </summary>
    /// <param name="errorInfo">The error information to report.</param>
    void Report(ErrorInfo errorInfo);

    /// <summary>
    /// Reports an exception as an error.
    /// </summary>
    /// <param name="exception">The exception to report as an error.</param>
    void Report(Exception exception);

    /// <summary>
    /// Subscribes to an error source and reports its errors.
    /// </summary>
    /// <param name="source">The error source to subscribe to.</param>
    /// <returns>A subscription that can be disposed to stop reporting errors from the source.</returns>
    IDisposable Report(IHasErrors source);

    /// <summary>
    /// Subscribes to multiple error sources and reports their errors.
    /// </summary>
    /// <typeparam name="T">The type of error sources, must implement <see cref="IHasErrors"/>.</typeparam>
    /// <param name="sources">The error sources to subscribe to.</param>
    /// <returns>A subscription that can be disposed to stop reporting errors from the sources.</returns>
    IDisposable ReportMany<T>(IObservableList<T> sources) where T : IHasErrors;

    /// <summary>
    /// Reports that last operation was successful, clearing LastError
    /// </summary>
    void ReportSuccess();

    /// <summary>
    /// Clears all reported errors.
    /// </summary>
    void Clear();
}