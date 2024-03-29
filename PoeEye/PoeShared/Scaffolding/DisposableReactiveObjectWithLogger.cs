﻿namespace PoeShared.Scaffolding;

public abstract class DisposableReactiveObjectWithLogger : DisposableReactiveObject
{
    private readonly Lazy<IFluentLog> logSupplier;
        
    protected DisposableReactiveObjectWithLogger()
    {
        logSupplier = new Lazy<IFluentLog>(PrepareLogger);
    }

    protected IFluentLog Log => logSupplier.Value;

    protected virtual IFluentLog PrepareLogger()
    {
        return GetType().PrepareLogger();
    }
}