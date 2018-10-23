using System;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Modularity
{
    public sealed class GenericConfigProvider<TConfig> : DisposableReactiveObject, IConfigProvider<TConfig> where TConfig : class, IPoeEyeConfig, new()
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GenericConfigProvider<TConfig>));

        private readonly IConfigProvider configProvider;
        private TConfig actualConfig;

        public GenericConfigProvider([NotNull] IConfigProvider configProvider)
        {
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));

            this.configProvider = configProvider;
            configProvider.ConfigHasChanged
                          .StartWith(Unit.Default)
                          .Subscribe(ReloadInternal)
                          .AddTo(Anchors);

            //FIXME Use ReplaySubject for propagating active config
            var changes = this
                .WhenAnyValue(x => x.ActualConfig)
                .WithPrevious((prev, curr) => new {prev, curr})
                .Select(x => new {Config = x.curr, PreviousConfig = x.prev, ComparisonResult = Compare(x.curr, x.prev)})
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
            configProvider.Reload();
        }

        public void Save(TConfig config)
        {
            Guard.ArgumentNotNull(config, nameof(config));

            configProvider.Save(config);
        }

        private ComparisonResult Compare(TConfig x, TConfig y)
        {
            return new CompareLogic(
                new ComparisonConfig
                {
                    MaxDifferences = byte.MaxValue
                }).Compare(x, y);
        }

        private void LogConfigChange(TConfig config, ComparisonResult result)
        {
            Log.Debug(
                $"[GenericConfigProvider.{typeof(TConfig).Name}] Config has changed:\nTime spent by comparer: {result.ElapsedMilliseconds}ms\n{result.DifferencesString}");
        }

        private void ReloadInternal()
        {
            ActualConfig = configProvider.GetActualConfig<TConfig>();
        }
    }
}