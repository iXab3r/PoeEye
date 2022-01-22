using System;
using System.Windows;

namespace PoeShared.Scaffolding.WPF;

/// <summary>
/// Base class for weak event managers.
/// </summary>
/// <typeparam name="TManager">The type of the weak event manager.</typeparam>
/// <typeparam name="TEventRaiser">The type of the class that raises the event.</typeparam>
/// <remarks>
/// Based on the idea presented by <a href="http://wekempf.spaces.live.com/">William Kempf</a>
/// in his article <a href="http://wekempf.spaces.live.com/blog/cns!D18C3EC06EA971CF!373.entry">WeakEventManager</a>.
/// </remarks>
public abstract class WeakEventManagerBase<TManager, TEventRaiser> : WeakEventManager
    where TManager : WeakEventManagerBase<TManager, TEventRaiser>, new()
    where TEventRaiser : class
{
    private static readonly Lazy<TManager> InstanceSupplier = new(() =>
    {
        var result = new TManager();
        SetCurrentManager(typeof(TManager), result);
        return result;
    });
        
    private static TManager Current => GetCurrentManager(typeof(TManager)) as TManager ?? InstanceSupplier.Value;

    public static void AddListener(TEventRaiser source, IWeakEventListener listener)
    {
        Current.ProtectedAddListener(source, listener);
    }

    public static void RemoveListener(TEventRaiser source, IWeakEventListener listener)
    {
        Current.ProtectedRemoveListener(source, listener);
    }

    protected override void StartListening(object source)
    {
        Start(source as TEventRaiser);
    }

    protected override void StopListening(object source)
    {
        Stop(source as TEventRaiser);
    }

    protected abstract void Start(TEventRaiser eventSource);
    protected abstract void Stop(TEventRaiser eventSource);
}