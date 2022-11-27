using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public abstract class ReactiveControl : Control, IDisposableReactiveObject
{
    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public CompositeDisposable Anchors { get; } = new();

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        RaisePropertyChanged(e.Property.Name);
    }
}