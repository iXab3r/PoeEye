namespace PoeShared.Utilities
{
    using System;
    using System.Reactive.Disposables;

    using ReactiveUI;

    public abstract class DisposableReactiveObject : ReactiveObject, IDisposable
    {
        protected CompositeDisposable Anchors { get; } = new CompositeDisposable();

        public void Dispose()
        {
            Anchors.Dispose();
        }
    }
}