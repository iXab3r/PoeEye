using ReactiveUI;

namespace PoeShared.Scaffolding
{
    using System;
    using System.ComponentModel;
    using System.Reactive.Disposables;

    using JetBrains.Annotations;

    public interface IDisposableReactiveObject : IDisposable, IReactiveObject
    {
        CompositeDisposable Anchors { [NotNull] get; }
    }
}