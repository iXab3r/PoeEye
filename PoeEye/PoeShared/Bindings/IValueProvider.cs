namespace PoeShared.Bindings;

/// <summary>
/// Values providers are responsible for providing an actual value OR show an exception if value retrieval is not possible
/// </summary>
public interface IValueProvider : IDisposableReactiveObject
{
    bool HasValue { get; }
        
    object Value { get; }
        
    Exception Error { get; }
}