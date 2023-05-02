using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public abstract class DisposableReactiveObject : IDisposableReactiveObject
{
    private readonly INpcEventInvoker propertyChanged;

    protected DisposableReactiveObject()
    {
        propertyChanged = new ConcurrentNpcEventInvoker(this);
    }

    public CompositeDisposable Anchors { get; } = new();

    public event PropertyChangedEventHandler PropertyChanged
    {
        add => propertyChanged.Add(value);
        remove => propertyChanged.Remove(value);
    }

    public void RaisePropertyChanged(string propertyName)
    {
        propertyChanged.Raise(propertyName);
    }

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
        Disposable.Create(() =>
        {
            foreach (var evaluator in itemsSupplier())
            {
                evaluator?.Dispose();
            }
        }).AddTo(Anchors);
    }
    
    protected void AddDisposeAction(Action action) 
    {
        Disposable.Create(action).AddTo(Anchors);
    }
    
    protected void AddDisposableResource<T>(Func<T> accessor) where T : IDisposable
    {
        Disposable.Create(() =>
        {
            var item = accessor();
            item?.Dispose();
        }).AddTo(Anchors);
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