using System;
using System.Threading;
using System.Windows.Controls;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.UI;

public sealed class TextBlockWithCounter : TextBlock
{
    public TextBlockWithCounter()
    {
        Count++;
        Thread.Sleep(100);
        Text = $"Block #{Count}";
    }

    public static int Count;
}

public interface IVirtualizedListContainer<T> where T : class
{
    T Value { get; set; }    
    
    Type ValueType { get; set; }
    
    bool HasValue { get; }
}

public class VirtualizedListContainer<T> : DisposableReactiveObject, IVirtualizedListContainer<T> where T : class
{
    private static readonly Binder<VirtualizedListContainer<T>> Binder = new();

    static VirtualizedListContainer()
    {
        Binder.Bind(x => x.Value != default).To(x => x.HasValue);
    }

    public VirtualizedListContainer()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public T Value { get; set; }
    public Type ValueType { get; set; }
    public int Index { get; set; }
    public bool HasValue { get; [UsedImplicitly] private set; }
}