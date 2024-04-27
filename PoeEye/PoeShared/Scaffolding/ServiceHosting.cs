using Microsoft.Extensions.Hosting;
using PoeShared.Prism;

namespace PoeShared.Scaffolding;

public sealed class ServiceHosting<T> : DisposableReactiveObjectWithLogger, IHostedService
{
    private static long globalInstanceId;
    
    private readonly IFactory<T> serviceFactory;
    private readonly SerialDisposable serviceAnchors;
    private readonly string instanceId = $"Hosted Service for {typeof(T).Name} #{Interlocked.Increment(ref globalInstanceId)}";

    public ServiceHosting(IFactory<T> serviceFactory)
    {
        Log = base.Log.WithSuffix(instanceId);
        this.serviceFactory = serviceFactory;
        serviceAnchors = new SerialDisposable().AddTo(Anchors);
        Disposable.Create(() => Log.Info($"Service host is disposed")).AddTo(Anchors);
    }
    
    private new IFluentLog Log { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Debug($"Start requested");
        var anchors = new CompositeDisposable().AssignTo(serviceAnchors);
        anchors.Add(Disposable.Create(() => Log.Info($"Anchors of service instance disposed")));
        try
        {
            Log.Debug($"Requesting instance");
            var service = serviceFactory.Create();
            Log.Debug($"Received instance: {service}");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to resolve instance", e);
            throw;
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Debug($"Stop requested");
        serviceAnchors.Disposable = default;
        return Task.CompletedTask;
    }
}