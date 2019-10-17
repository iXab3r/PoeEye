using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using JetBrains.Annotations;
using ReactiveUI;

namespace PoeShared.Scaffolding
{
    public interface IDisposableReactiveObject : IDisposable, INotifyPropertyChanged
    {
        CompositeDisposable Anchors { [NotNull] get; }

        void RaisePropertyChanged([CanBeNull] string propertyName);
    }
}