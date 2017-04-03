using System;
using System.Reactive.Disposables;
using Guards;

namespace PoeShared.Scaffolding
{
    public static class DisposableExtensions
    {
        public static T AssignTo<T>(this T instance, SerialDisposable anchor) where T : IDisposable
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(anchor, nameof(anchor));

            anchor.Disposable = instance;
            return instance;
        }
    }
}
