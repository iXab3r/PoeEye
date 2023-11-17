using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

/// <summary>
/// Represents an object that supports both disposability and reactive property changes.
/// </summary>
public abstract class DisposableReactiveObject : IDisposableReactiveObject
{
    private readonly INpcEventInvoker propertyChanged;

    protected DisposableReactiveObject()
    {
        propertyChanged = new ConcurrentNpcEventInvoker(this);
    }

    /// <inheritdoc />
    public CompositeDisposable Anchors { get; } = new();

    public event PropertyChangedEventHandler PropertyChanged
    {
        add => propertyChanged.Add(value);
        remove => propertyChanged.Remove(value);
    }

    /// <inheritdoc />
    public void RaisePropertyChanged(string propertyName)
    {
        propertyChanged.Raise(propertyName);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }

    [UsedImplicitly]
    protected TRet RaiseAndSetIfChanged<TRet>(ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
        {
            return newValue;
        }

        return RaiseAndSet(ref backingField, newValue, propertyName);
    }

    protected TRet RaiseAndSet<TRet>(
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string propertyName = null)
    {
        backingField = newValue;
        RaisePropertyChanged(propertyName);
        return newValue;
    }

    protected void AddDisposableResources<T>(Func<IEnumerable<T>> itemsSupplier) where T : IDisposable
    {
        Anchors.Add(itemsSupplier);
    }
    
    protected void AddDisposableResource<T>(Func<T> accessor) where T : IDisposable
    {
        Anchors.Add(accessor);
    }
    
    protected void EnsureNotDisposed()
    {
        if (Anchors.IsDisposed)
        {
            throw new ObjectDisposedException($"Object is already disposed: {this}");
        }
    }

    protected void RaisePropertyChanged(params string[] properties)
    {
        properties.ForEach(RaisePropertyChanged);
    }

    public sealed override string ToString()
    {
        var builder = new ToStringBuilder(this);
        FormatToString(builder);
        return builder.ToString();
    }

    protected virtual void FormatToString(ToStringBuilder builder)
    {
    }

    protected static void EnsureUiThread()
    {
        if (!IsOnUiThread)
        {
            throw new InvalidOperationException($"Operation must be completed on UI thread");
        }
    }
        
    protected static void EnsureNonUiThread()
    {
        if (IsOnUiThread)
        {
            throw new InvalidOperationException($"Operation must be completed on non-UI thread");
        }
    }

    protected static bool IsOnUiThread => Environment.CurrentManagedThreadId == 1;
}