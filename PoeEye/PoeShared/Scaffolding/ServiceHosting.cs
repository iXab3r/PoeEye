using Microsoft.Extensions.Hosting;
using PoeShared.Prism;

namespace PoeShared.Scaffolding;

public sealed class ServiceHosting<T> : DisposableReactiveObjectWithLogger, IHostedService
{
    private static long globalInstanceId;
    
    private readonly IFactory<T> serviceFactory;
    private readonly SerialDisposable serviceAnchors;
    private readonly string instanceId = $"Hosted Service for {typeof(T).Name} #{Interlocked.Increment(ref globalInstanceId)}";

    private CancellationTokenSource serviceTokenSource;
    private T serviceInstance;

    public ServiceHosting(IFactory<T> serviceFactory)
    {
        Log = base.Log.WithSuffix(instanceId);
        this.serviceFactory = serviceFactory;
        serviceAnchors = new SerialDisposable().AddTo(Anchors);
        Disposable.Create(() => Log.Info($"Service host is disposed")).AddTo(Anchors);
    }
    
    private new IFluentLog Log { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Debug($"Start requested");
        var anchors = new CompositeDisposable().AssignTo(serviceAnchors);
        anchors.Add(Disposable.Create(() => Log.Info($"Anchors of service instance disposed")));
        
        try
        {
            Log.Debug($"Requesting instance");
            serviceTokenSource = new CancellationTokenSource();
            var service = serviceFactory.Create();
            Log.Debug($"Received instance: {service}");
            serviceInstance = service;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to resolve instance", e);
            throw;
        }
        
        if (serviceInstance is IDisposable disposableService)
        {
            anchors.Add(Disposable.Create(() =>
            {
                Log.Info($"Disposing service {disposableService}");
                disposableService.Dispose();
            }));
        }
            
        if (serviceInstance is IHostedService hostedService)
        {
            Log.Info($"Starting hosted service {hostedService}");
            await hostedService.StartAsync(serviceTokenSource.Token);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Debug($"Stop requested");
        
        if (serviceInstance is IHostedService hostedService)
        {
            Log.Info($"Stopping hosted service {hostedService}");
            await hostedService.StopAsync(serviceTokenSource.Token);
        }
        serviceAnchors.Disposable = default;
        serviceInstance = default;
        serviceTokenSource = default;
    }
}