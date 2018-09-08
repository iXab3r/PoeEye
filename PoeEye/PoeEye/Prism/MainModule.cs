using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using Guards;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using PoeEye.Config;
using PoeEye.Converters;
using PoeEye.PoeTrade.ViewModels;
using PoeShared;
using PoeShared.Common;
using PoeShared.Communications.Chromium;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using Prism.Modularity;
using Prism.Unity;

namespace PoeEye.Prism
{
    internal sealed class MainModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public MainModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new UiRegistrations());
            
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeEyeMainConfig, PoeMainSettingsViewModel>();
            registrator.RegisterSettingsEditor<PoeEyeUpdateSettingsConfig, PoeEyeUpdateSettingsViewModel>();

            InitializeConfigConverters();
            
            Log.Instance.Info($"Initializing Chromium...");
            var chromium = container.Resolve<IChromiumBootstrapper>();
            chromium.AddTo(anchors);
        }
        
        private void InitializeConfigConverters()
        {
            var configProvider = container.TryResolve<IConfigSerializer>();
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
