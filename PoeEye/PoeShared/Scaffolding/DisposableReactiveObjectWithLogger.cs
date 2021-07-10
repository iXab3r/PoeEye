using System;
using PoeShared.Logging;

namespace PoeShared.Scaffolding
{
    public abstract class DisposableReactiveObjectWithLogger : DisposableReactiveObject
    {
        private readonly Lazy<IFluentLog> logSupplier;
        
        protected DisposableReactiveObjectWithLogger()
        {
            logSupplier = new Lazy<IFluentLog>(PrepareLogger);
        }

        protected IFluentLog Log => logSupplier.Value;

        protected abstract IFluentLog PrepareLogger();
    }
}