using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows.Forms;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public class ReactiveForm : Form, IDisposableReactiveObject
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Anchors.Dispose();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public CompositeDisposable Anchors { get; } = new();

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}