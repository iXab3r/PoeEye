using System;
using System.Reactive.Disposables;
using Common.Logging;
using Guards;
using Newtonsoft.Json;
using PoeEye.Config;
using PoeEye.Converters;
using PoeEye.PoeTrade.ViewModels;
using PoeEye.Settings.ViewModels;
using PoeShared.Chromium.Communications;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using Prism.Ioc;
using Unity;

namespace PoeEye.Prism
{
    internal sealed class MainModule : IPoeEyeModule
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainModule));

        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly IUnityContainer container;

        public MainModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new UiRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();

            registrator.RegisterSettingsEditor<PoeEyeMainConfig, PoeMainSettingsViewModel>();
            registrator.RegisterSettingsEditor<PoeEyeUpdateSettingsConfig, PoeEyeUpdateSettingsViewModel>();

            InitializeConfigConverters();

            Log.Info("Initializing Chromium core...");
            var chromium = container.Resolve<IChromiumBootstrapper>();
            chromium.AddTo(anchors);
        }

        private void InitializeConfigConverters()
        {
            var configProvider = container.Resolve<IConfigSerializer>();
            var converters = new JsonConverter[]
            {
                new ConcreteTypeConverter<IPoeQueryInfo, PoeQueryInfo>(),
                new ConcreteTypeConverter<IPoeItemType, PoeItemType>(),
                new ConcreteTypeConverter<IPoeItem, PoeItem>(),
                new ConcreteTypeConverter<IPoeItemMod, PoeItemMod>(),
                new ConcreteTypeConverter<IPoeLinksInfo, PoeLinksInfo>(),
                new ConcreteTypeConverter<IPoeQueryModsGroup, PoeQueryModsGroup>(),
                new ConcreteTypeConverter<IPoeQueryRangeModArgument, PoeQueryRangeModArgument>()
            };
            Array.ForEach(converters, configProvider.RegisterConverter);
        }
    }
}