using System;
using Common.Logging.Configuration;
using Guards;
using ReactiveUI;
using ReactiveUI.Legacy;

namespace PoeShared.Scaffolding
{
    public static class ReactiveCommandExtensions
    {
        public static ReactiveCommand<T> SubscribeToExceptions<T>(
            this ReactiveCommand<T> instance, 
            Action<Exception> onNextError,
            Action<IDisposable> anchorProcessor)
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(onNextError, nameof(onNextError));

            var anchor = instance.ThrownExceptions.Subscribe(onNextError);
            anchorProcessor(anchor);
            return instance;
        }
    }
}
