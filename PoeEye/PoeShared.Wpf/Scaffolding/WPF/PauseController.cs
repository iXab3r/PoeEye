using System;
using JetBrains.Annotations;
using PoeShared.Services;
using PropertyBinder;

namespace PoeShared.Scaffolding.WPF;

public sealed class PauseController : DisposableReactiveObject, IPauseController
{
    private static readonly Binder<PauseController> Binder = new();

    private readonly ISharedResourceLatch resourceLatch = new SharedResourceLatch();

    static PauseController()
    {
        Binder.Bind(x => x.resourceLatch.IsBusy).To(x => x.IsPaused);
    }

    public PauseController()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public bool IsPaused { get; [UsedImplicitly] private set; }
    
    public IDisposable Pause()
    {
        return resourceLatch.Rent();
    }
}