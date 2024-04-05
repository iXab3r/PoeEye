using System.Diagnostics;
using System.Threading;
using App.Metrics;
using App.Metrics.Gauge;
using PoeShared.Modularity;

namespace PoeShared.Logging;

internal sealed class MetricsService : DisposableReactiveObject
{
    private static readonly IFluentLog Log = typeof(MetricsService).PrepareLogger();

    private static readonly Lazy<MetricsService> InstanceSupplier = new();
        
    private readonly Lazy<IMetricsRoot> rootSupplier;
    private readonly GaugeOptions appLifetimeCounter = new GaugeOptions() { Name = "Application lifetime", MeasurementUnit = Unit.Custom("ms")};
    private IAppArguments appArguments;
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public MetricsService()
    {
        Log.Debug("Metrics service created");
        rootSupplier = new Lazy<IMetricsRoot>(() => InitializeMetrics(appArguments));
    }

    public static MetricsService Instance => InstanceSupplier.Value;

    public IMetricsRoot Metrics => rootSupplier.Value;

    public void Initialize(IAppArguments appArguments)
    {
        Log.Debug($"Initializing metrics service, args: {appArguments}");
        if (this.appArguments != default)
        {
            throw new InvalidOperationException($"Service is already initialized");
        }
        this.appArguments = appArguments;

        Observables.BlockingTimer( TimeSpan.FromSeconds(1))
            .SubscribeSafe(() => Metrics.Measure.Gauge.SetValue(appLifetimeCounter, stopwatch.ElapsedMilliseconds), Log.HandleException)
            .AddTo(Anchors);
            
        Observables.BlockingTimer(TimeSpan.FromSeconds(30))
            .Subscribe(async idx =>
            {
                try
                {
                    ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
                    Log.Debug($"Reporting metrics #{idx}, available thread pool threads: worker {workerThreads}, completionPort {completionPortThreads}");
                    await Task.WhenAll(Metrics.ReportRunner.RunAllAsync());
                }
                catch (Exception e)
                {
                    Log.Warn($"Failed to report metrics #{idx}", e);
                }
            })
            .AddTo(Anchors);
    }
        
    private static IMetricsRoot InitializeMetrics(IAppArguments appArguments)
    {
        if (appArguments == null)
        {
            throw new ArgumentNullException($"App arguments are not initialized");
        }
        Log.Info("Initializing metrics...");

        if (!Log.IsInfoEnabled)
        {
            return new MetricsBuilder().Build();
        }

        var metricsOutput = Path.Combine(appArguments.AppDataDirectory, $"logs", $"metrics.txt");
        Log.Info($"Exporting metrics to file {metricsOutput}");
        var metrics = new MetricsBuilder()
            .Configuration.Configure(
                options =>
                {
                    options.Enabled = true;
                    options.ReportingEnabled = true;
                })
            .Report.ToTextFile(options =>
            {
                options.OutputPathAndFileName = metricsOutput;
                options.AppendMetricsToTextFile = false;
            })
            .Build();
        return metrics;
    }
}