namespace PoeShared.Utilities
{
    using System;
    using System.Reactive.Disposables;

    public static class CompositeDisposableExtensions
    {
        public static IDisposable AddTo(this IDisposable anchor, CompositeDisposable anchors)
        {
            anchors.Add(anchor);
            return anchor;
        }
    }
}