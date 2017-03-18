using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.Config
{
    internal sealed class GenericConfigProvider<TConfig> : DisposableReactiveObject, IConfigProvider<TConfig> where TConfig : class, IPoeEyeConfig, new()
    {
        private readonly IConfigProvider configProvider;
        private Lazy<TConfig> configLoader;

        public GenericConfigProvider([NotNull] IConfigProvider configProvider)
        {
            Guard.ArgumentNotNull(() => configProvider);

            this.configProvider = configProvider;
            configProvider.ConfigHasChanged
                .StartWith(Unit.Default)
                .Subscribe(ReloadInternal)
                .AddTo(Anchors);

            //FIXME Use ReplaySubject for propagating active config
            WhenChanged = this
                .WhenAnyValue(x => x.ActualConfig)
                .WithPrevious((prev, curr) => new { prev, curr })
                .Select(x => new { Config = x.curr, PreviousConfig = x.prev, ComparisonResult = Compare(x.curr, x.prev) })
                .Where(x => !x.ComparisonResult.AreEqual)
                .Do(
                    x =>
                    {
                        if (x.PreviousConfig != null)
                        {
                            LogConfigChange(x.Config, x.ComparisonResult);
                        }
                    })
                .Select(x => x.Config); ;
        }

        public TConfig ActualConfig => configLoader.Value;

        public IObservable<TConfig> WhenChanged { get; }

        public IObservable<T> ListenTo<T>(Expression<Func<TConfig, T>> fieldToMonitor)
        {
            return
                this.WhenAnyValue(x => x.ActualConfig)
                    .Select(config => fieldToMonitor.Compile().Invoke(config))
                    .DistinctUntilChanged();
        }

        public void Reload()
        {
            configProvider.Reload();
        }

        public void Save(TConfig config)
        {
            Guard.ArgumentNotNull(() => config);

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
            Log.Instance.Debug($"[GenericConfigProvider.{typeof(TConfig).Name}] Config has changed:\nTime spent by comparer: {result.ElapsedMilliseconds}ms\n{result.DifferencesString}");
        }

        private void ReloadInternal()
        {
            configLoader = new Lazy<TConfig>(() => configProvider.GetActualConfig<TConfig>());
            this.RaisePropertyChanged(nameof(ActualConfig));
        }
    }
}