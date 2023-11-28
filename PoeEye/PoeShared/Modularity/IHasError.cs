using JetBrains.Annotations;

namespace PoeShared.Modularity;

public interface IHasError
{
    /// <summary>
    /// Gets the last error that occurred.
    /// </summary>
    ErrorInfo? LastError { [CanBeNull] get; }
}