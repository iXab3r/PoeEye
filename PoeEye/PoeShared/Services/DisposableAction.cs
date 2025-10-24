#nullable enable
namespace PoeShared.Services;

/// <summary>
/// This helper class provides a simple way to create a disposable action either using Pooling via <see cref="DisposablePooledAction"/> or directly via Disposable.Create
/// </summary>
public sealed class DisposableAction 
{
    /// <summary>
    /// If true, will attempt to optimize memory usage by pooling instances of Disposable actions via <see cref="DisposablePooledAction"/>.
    /// </summary>
    public static bool UsePooling { get; set; }
    
    public static IDisposable Create(Action action)
    {
        return UsePooling 
            ? DisposablePooledAction.Create(action) 
            : Disposable.Create(action);
    }
}