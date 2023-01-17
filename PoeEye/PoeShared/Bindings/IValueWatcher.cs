namespace PoeShared.Bindings;

/// <summary>
/// Value watchers are responsible for not only providing value/error but are able to set current value in some cases
/// </summary>
public interface IValueWatcher : IValueProvider
{
    object Source { get; set; }
        
    /// <summary>
    /// True if is is possible to set current value, requires SupportsSetValue and Source to be non-null
    /// </summary>
    bool CanSetValue { get; }
        
    /// <summary>
    /// True if this value watcher knows how to set current Value
    /// </summary>
    bool SupportsSetValue { get; }
        
    /// <summary>
    /// Attempts to set current value, should throw only in extreme cases (e.g. misconfiguration), otherwise sets Error
    /// </summary>
    /// <param name="newValue"></param>
    void SetCurrentValue(object newValue);
}