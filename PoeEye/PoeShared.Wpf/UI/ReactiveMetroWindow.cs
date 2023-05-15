using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Interop;
using MahApps.Metro.Controls;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public class ReactiveMetroWindow : MetroWindow
{
    public CompositeDisposable Anchors { get; } = new();

    public ReactiveMetroWindow()
    {
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
        Controller = new WindowViewController(this).AddTo(Anchors);
    }

    private void OnSourceInitialized(object sender, EventArgs e)
    {
        WindowHandle = new WindowInteropHelper(this).Handle; // should be already available here
        if (WindowHandle == IntPtr.Zero)
        {
            throw new InvalidStateException("Window handle must be initialized at this point");
        }
    }

    public IntPtr WindowHandle { get; private set; }
    
    public IWindowViewController Controller { get; }

    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        RaisePropertyChanged(e.Property.Name);
    }

    private void OnClosed(object sender, EventArgs e)
    {
        Dispose();
    }
}