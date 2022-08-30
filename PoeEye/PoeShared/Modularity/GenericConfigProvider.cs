using System.Reactive.Subjects;
using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;
using PoeShared.Services;
using ReactiveUI;

namespace PoeShared.Modularity;

public sealed class GenericConfigProvider<TConfig> : DisposableReactiveObjectWithLogger, IConfigProvider<TConfig> where TConfig : class, IPoeEyeConfig, new()
{
    private readonly IComparisonService comparisonService;
    private readonly IConfigProvider configProvider;
    private readonly Subject<string> reloadSignal = new(); 

    private int saveCommandCounter = 0;
    private int loadCommandCounter = 0;

    public GenericConfigProvider(
        [NotNull] IComparisonService comparisonService,
        [NotNull] IConfigProvider configProvider)
    {
        Guard.ArgumentNotNull(comparisonService, nameof(comparisonService));
        Guard.ArgumentNotNull(configProvider, nameof(configProvider));

        Log.Debug($"Initializing config provider for {typeof(TConfig)}");

        this.comparisonService = comparisonService;
        this.configProvider = configProvider;

        configProvider.ConfigHasChanged
            .Select(x => $"Config change reported by provider {configProvider}")
            .Subscribe(reloadSignal)
            .AddTo(Anchors);
            
        reloadSignal
            .StartWithDefault()
            .Select(
                x =>
                {
                    Log.Debug(() => $"Refreshing ActualConfig...");
                    var result = configProvider.GetActualConfig<TConfig>();
                    Log.Debug(() => "Refreshed actual config");
                    return result;
                })
            .Subscribe(x => ActualConfig = x)
            .AddTo(Anchors);

        var changes = this
            .WhenAnyValue(x => x.ActualConfig)
            .WithPrevious((prev, curr) => new {prev, curr})
            .Do(x =>
            {
                if (ReferenceEquals(x.prev, x.curr))
                {
                    throw new ApplicationException($"Previous config instance is equal to the current one ! Instance: {x}");
                }
            })
            .Select(x => new {Config = x.curr, PreviousConfig = x.prev, ComparisonResult = comparisonService.Compare(x.curr, x.prev)})
            .Do(x => { LogActualConfigChange(x.PreviousConfig, x.Config, x.ComparisonResult); })
            .Where(x => !x.ComparisonResult.AreEqual)
            .Select(x => x.Config)
            .Catch<TConfig, Exception>(ex =>
            {
                Log.HandleException(ex);
                return Observable.Never<TConfig>();
            })
            .Replay(1);

        WhenChanged = changes;
        changes.Connect().AddTo(Anchors);
            
        Log.Debug(() => $"Initial re-save of config to update format using {configProvider}");
        configProvider.Save();
    }
        
    public TConfig ActualConfig { get; private set; }

    public IObservable<TConfig> WhenChanged { get; }

    public IObservable<T> ListenTo<T>(Expression<Func<TConfig, T>> fieldToMonitor)
    {
        var functor = fieldToMonitor.Compile();
        return
            WhenChanged
                .Select(config => functor(config))
                .DistinctUntilChanged();
    }

    public void Save(TConfig config)
    {
        Guard.ArgumentNotNull(config, nameof(config));

        var compare = comparisonService.Compare(ActualConfig, config);

        if (compare.AreEqual)
        {
            Log.Debug(() => $"Attempted to save config that is an exact duplicate of an Actual config, skipping request");
            return;
        }

        Interlocked.Increment(ref saveCommandCounter);
        Log.Debug(() => $"ConfigProvider Save/Load stat: { new { saveCommandCounter, loadCommandCounter } }");

        configProvider.Save(config);
        reloadSignal.OnNext("Reloading after Save");
    }
        
    private void LogActualConfigChange(TConfig previousConfig, TConfig currentConfig, ComparisonResult result)
    {
        Log.Debug(() => $"Actual config updated(areEqual: {result.AreEqual})\nPrevious: {(previousConfig == null ? "NULL" : previousConfig.Dump())}\nCurrent: {(currentConfig == null ? "NULL" : currentConfig.Dump())}\nTime spent by comparer: {result.ElapsedMilliseconds}ms\n{result.DifferencesString}");
    }
}