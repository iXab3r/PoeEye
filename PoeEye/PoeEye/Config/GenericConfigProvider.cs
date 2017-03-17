using System;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
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
        }

        public TConfig ActualConfig => configLoader.Value;

        public IObservable<T> ListenTo<T>(Expression<Func<TConfig, T>> fieldToMonitor)
        {
            return
                this.WhenAnyValue(x => x.ActualConfig)
                    .Select(config => config.WhenAnyValue(fieldToMonitor))
                    .Switch();
        }

        public void Reload()
        {
            configProvider.Reload();
        }

        public void Save(TConfig config)
        {
            Guard.ArgumentNotNull(() => config);

            var actualConfig = configProvider.GetActualConfig<TConfig>();
            config.TransferPropertiesTo(actualConfig);

            configProvider.Save();
        }

        private void ReloadInternal()
        {
            configLoader = new Lazy<TConfig>(() => configProvider.GetActualConfig<TConfig>());
            this.RaisePropertyChanged(nameof(ActualConfig));
        }
    }
}