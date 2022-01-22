using System.Reactive.Disposables;
using System.Windows.Controls;

namespace PoeShared.UI;

public abstract class ReactiveControl : Control
{
    public CompositeDisposable Anchors { get; } = new CompositeDisposable();
}