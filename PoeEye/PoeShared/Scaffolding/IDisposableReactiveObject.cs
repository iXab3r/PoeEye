namespace PoeShared.Scaffolding
{
    using System;
    using System.ComponentModel;
    using System.Reactive.Disposables;

    using JetBrains.Annotations;

    public interface IDisposableReactiveObject : IDisposable, INotifyPropertyChanged
    {
        CompositeDisposable Anchors { [NotNull] get; }
    }
}