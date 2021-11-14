using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using App.Metrics;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.Logging
{
    internal sealed class MetricsService : DisposableReactiveObject
    {
        private static readonly IFluentLog Log = typeof(MetricsService).PrepareLogger();

        private static readonly Lazy<MetricsService> InstanceSupplier = new();
        
        private readonly Lazy<IMetricsRoot> rootSupplier;
        private IAppArguments appArguments;

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
            
            Observable.Timer(DateTime.Now, TimeSpan.FromSeconds(30))
                .Subscribe(async idx =>
                {
                    try
                    {
                        Log.Debug(() => $"Reporting metrics #{idx}");
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

            var metricsOutput = Path.Combine(appArguments.AppDataDirectory, $"logs", $"metrics{(appArguments.IsDebugMode ? "DebugMode" : default)}.txt");
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
}