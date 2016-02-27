namespace PoeShared.Scaffolding
{
    using System;
    using System.Reactive.Disposables;

    using ReactiveUI;

    public abstract class DisposableReactiveObject : ReactiveObject, IDisposable
    {
        protected CompositeDisposable Anchors { get; } = new CompositeDisposable();

        public virtual void Dispose()
        {
            Anchors.Dispose();
        }
    }
}