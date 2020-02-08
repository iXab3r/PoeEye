﻿using log4net;
using PoeShared.Modularity;
using PoeShared.Squirrel.Updater;
using PoeShared.Wpf.Scaffolding;
using Prism.Ioc;
using Prism.Modularity;
using Unity;

namespace PoeShared.Squirrel.Prism
{
    public sealed class UpdaterModule : IModule
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UpdaterModule));

        private readonly IUnityContainer container;

        public UpdaterModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddExtension(new UpdaterRegistrations());
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<UpdateSettingsConfig, UpdateSettingsViewModel>();
        }
    }
}