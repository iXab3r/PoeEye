using System;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;
using log4net;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Services;
using ReactiveUI;
using Unity;

namespace PoeShared.Modularity
{
    public sealed class GenericConfigProvider<TConfig> : DisposableReactiveObject, IConfigProvider<TConfig> where TConfig : class, IPoeEyeConfig, new()
    {
        private readonly IComparisonService comparisonService;
        private readonly IConfigProvider configProvider;
        
        private int saveCommandCounter = 0;
        private int loadCommandCounter = 0;

        public GenericConfigProvider(
            [NotNull] IComparisonService comparisonService,
            [NotNull] IConfigProvider configProvider)
        {
            Guard.ArgumentNotNull(comparisonService, nameof(comparisonService));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));

            Log = GetType().PrepareLogger();

            this.comparisonService = comparisonService;
            this.configProvider = configProvider;
            
            configProvider
                .ConfigHasChanged
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
        
        private IFluentLog Log { get; }

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

        public void Reload()
        {
            Interlocked.Increment(ref loadCommandCounter);
            Log.Debug(() => $"ConfigProvider Save/Load stat: { new { saveCommandCounter, loadCommandCounter } }");

            configProvider.Reload();
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
        }
        
        private void LogActualConfigChange(TConfig previousConfig, TConfig currentConfig, ComparisonResult result)
        {
            Log.Debug(() => $"Actual config updated(areEqual: {result.AreEqual})\nPrevious: {(previousConfig == null ? "NULL" : previousConfig.DumpToTextRaw())}\nCurrent: {(currentConfig == null ? "NULL" : currentConfig.DumpToTextRaw())}\nTime spent by comparer: {result.ElapsedMilliseconds}ms\n{result.DifferencesString}");
        }
    }
}