using System;
using System.Reactive.Disposables;
using JetBrains.Annotations;
using ReactiveUI;

namespace PoeShared.Scaffolding
{
    public interface IDisposableReactiveObject : IDisposable, IReactiveObject
    {
        CompositeDisposable Anchors { [NotNull] get; }
    }
}