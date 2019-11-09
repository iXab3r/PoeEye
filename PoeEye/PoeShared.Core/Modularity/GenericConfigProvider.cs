using System;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;
using log4net;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace PoeShared.Modularity
{
    public sealed class GenericConfigProvider<TConfig> : DisposableReactiveObject, IConfigProvider<TConfig> where TConfig : class, IPoeEyeConfig, new()
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GenericConfigProvider<TConfig>));

        private readonly IConfigProvider configProvider;
        private TConfig actualConfig;
        
        private int saveCommandCounter = 0;
        private int loadCommandCounter = 0;

        public GenericConfigProvider(
            [NotNull] IConfigProvider configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.configProvider = configProvider;
            configProvider.ConfigHasChanged
                .ObserveOn(uiScheduler)
                .StartWith(Unit.Default)
                .Subscribe(ReloadInternal)
                .AddTo(Anchors);

            //FIXME Use ReplaySubject for propagating active config
            var changes = this
                .WhenAnyValue(x => x.ActualConfig)
                .WithPrevious((prev, curr) => new {prev, curr})
                .Do(x =>
                {
                    if (ReferenceEquals(x.prev, x.curr))
                    {
                        throw new ApplicationException($"Previous config instance is equal to the current one ! Instance: {x.DumpToTextRaw()}");
                    }
                })
                .Select(x => new {Config = x.curr, PreviousConfig = x.prev, ComparisonResult = Compare(x.curr, x.prev)})
                .Do(x => { LogActualConfigChange(x.PreviousConfig, x.Config, x.ComparisonResult); })
                .Where(x => !x.ComparisonResult.AreEqual)
                .Do(
                    x =>
                    {
                        if (x.PreviousConfig != null)
                        {
                            LogConfigChange(x.Config, x.ComparisonResult);
                        }
                    })
                .Select(x => x.Config)
                .Catch<TConfig, Exception>(ex =>
                {
                    Log.HandleException(ex);
                    return Observable.Never<TConfig>();
                })
                .Replay(1);

            WhenChanged = changes;
            changes.Connect().AddTo(Anchors);
        }

        public TConfig ActualConfig
        {
            get => actualConfig;
            private set => this.RaiseAndSetIfChanged(ref actualConfig, value);
        }

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
            Log.Info($"Current stat: { new { saveCommandCounter, loadCommandCounter } }");

            configProvider.Reload();
        }

        public void Save(TConfig config)
        {
            Guard.ArgumentNotNull(config, nameof(config));

            Interlocked.Increment(ref saveCommandCounter);
            Log.Info($"Current stat: { new { saveCommandCounter, loadCommandCounter } }");

            configProvider.Save(config);
        }

        private ComparisonResult Compare(TConfig x, TConfig y)
        {
            return new CompareLogic(
                new ComparisonConfig
                {
                    DoublePrecision = 0.01,
                    MaxDifferences = byte.MaxValue
                }).Compare(x, y);
        }

        private void LogConfigChange(TConfig config, ComparisonResult result)
        {
            Log.Debug(
                $"[{typeof(TConfig).Name}] Config has changed:{config.DumpToTextRaw()}\nTime spent by comparer: {result.ElapsedMilliseconds}ms\n{result.DifferencesString}");
        }
        
        private void LogActualConfigChange(TConfig previousConfig, TConfig currentConfig, ComparisonResult result)
        {
            Log.Debug(
                $"[{typeof(TConfig).Name}] Actual config updated(areEqual: {result.AreEqual})\nPrevious: {(previousConfig == null ? "NULL" : previousConfig.DumpToTextRaw())}\nCurrent: {(currentConfig == null ? "NULL" : currentConfig.DumpToTextRaw())}\nTime spent by comparer: {result.ElapsedMilliseconds}ms\n{result.DifferencesString}");
        }

        private void ReloadInternal()
        {
            ActualConfig = configProvider.GetActualConfig<TConfig>();
        }
    }
}