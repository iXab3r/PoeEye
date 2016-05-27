using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using JetBrains.Annotations;

namespace PoePickitTestApp
{
    public interface IDisposableReactiveObject : IDisposable, INotifyPropertyChanged
    {
        CompositeDisposable Anchors { [NotNull] get; }
    }
}